
using PBApplication.Context.Abstractions;
using PBApplication.Events;
using PBApplication.Extensions;
using PBApplication.Requests.Abstractions;
using PBApplication.Responses;
using PBApplication.Responses.Abstractions;
using PBApplication.Services;
using PBCommon.Extensions;
using PBCommon.Validation;
using PBData.Entities;
using PBData.Extensions;
using UnityShuffle.Data;
using UnityShuffle.Services.Abstractions;

namespace UnityShuffle.Services
{
	public sealed class MissionService : DBConnectedService, IMissionService
	{
		public MissionService(IServiceContext serviceContext) : base(serviceContext)
		{
			Observe<IMissionService>(this);
		}

		public event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionAdded;
		public event ServiceEventHandler<ServiceEventArgs>? MissionRemoved;
		public event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionUpdated;

		public async Task<IResponse> AddMission(IMissionService.AddMissionRequest request)
		{
			var response = new Response();

			Boolean validName()
			{
				return !String.IsNullOrWhiteSpace(request.Name);
			}

			async Task notNullRequest()
			{
				Boolean validLocation()
				{
					return Common.Settings.Locations.Contains(request.Location);
				}
				Boolean validAspects()
				{
					return request.Aspects.All(a => Common.Settings.Locations.Contains(a));
				}
				Boolean validDescription()
				{
					return !String.IsNullOrWhiteSpace(request.Description);
				}

				Expire();

				String name = request.Name.ToLower();
				MissionEntity duplicate = Connection.GetSingle<MissionEntity>(m=>m.Name.ToLower().Equals(name));

				void successAction()
				{
					var newMission = new MissionEntity(request.Name,
														request.Description,
														request.Location,
														Session.User,
														Common.Settings.MissionLifespan);
					newMission.RefreshNow();

					Connection.Insert(newMission);
					Connection.SaveChanges();

					MissionAdded.Invoke(Session, Common.Settings.MissionAddedHubId, newMission.CloneAsT());
				}

				await FirstValidateAuthenticated()
					.NextCompound(validLocation,
						response.Validation.GetField(nameof(request.Location)),
						ValidationCode.Invalid)
					.NextCompound(validAspects,
						response.Validation.GetField(nameof(request.Aspects)),
						ValidationCode.Invalid)
					.NextCompound(validDescription,
						response.Validation.GetField(nameof(request.Description)),
						ValidationCode.Invalid)
					.NextNullCheck(duplicate,
						response.Validation.GetField(nameof(request.Name)),
						ValidationCode.Duplicate)
					.InvertCriterion()
					.SetOnCriterionMet(successAction)
					.Evaluate(response);
			}

			await FirstRequestNullCheck(request, response)
				.NextCompound(validName,
					response.Validation.GetField(nameof(request.Name)),
					ValidationCode.Invalid)
				.SetOnCriterionMet(notNullRequest)
				.CatchAll(ValidationField.Request)
				.Evaluate(response);

			return response;
		}

		public async Task<IGetPaginatedEncryptableResponse<MissionEntity>> GetMissions(IGetPaginatedRequest<IMissionService.GetMissionRequest> request)
		{
			var response = new GetPaginatedEncryptableResponse<MissionEntity>();

			async Task notNullRequest()
			{
				Expire();

				IEnumerable<MissionEntity> query = Connection.Query<MissionEntity>();

				if (!String.IsNullOrWhiteSpace(request.Parameter.Name))
				{
					String name = request.Parameter.Name.ToLower();
					query = query.Where(m => m.Name.ToLower().Contains(name));
				}
				if (!String.IsNullOrWhiteSpace(request.Parameter.Location))
				{
					String location = request.Parameter.Location.ToLower();
					query = query.Where(m => m.Location.ToLower().Equals(location));
				}
				if (!String.IsNullOrWhiteSpace(request.Parameter.Description))
				{
					String description = request.Parameter.Description.ToLower();
					query = query.Where(m => m.Description.ToLower().Equals(description));
				}
				if (request.Parameter.Aspects?.Any() ?? false)
				{
					String lowerCaseAspect = String.Empty;
					foreach (var aspect in request.Parameter.Aspects)
					{
						lowerCaseAspect = aspect.ToLower();
						query = query.Where(m => m.Aspects.Contains(lowerCaseAspect));
					}
				}
				if (request.Parameter.MaxTime.HasValue)
				{
					Int64 max = request.Parameter.MaxTime.Value.Ticks;
					query = query.Where(m => m.Ratings.Average(r => r.TimeTaken.Ticks) <= max);
				}

				query = query.OrderBy(m => m.CreationDate);

				void successAction()
				{
					response.LastPage = query.GetPageCount(request.PerPage) - 1;
					response.Data = query.Paginate(request.PerPage, request.Page)
						.CloneAsT()
						.ToList();
				}

				await CachedCriterionChain.Cache.Get()
					.NextValidatePagination(request, query)
					.SetOnCriterionMet(successAction)
					.Evaluate(response);
			}

			await FirstParameterizedRequestNullCheck(request, response)
				.SetOnCriterionMet(notNullRequest)
				.Evaluate(response);

			return response;
		}

		public async Task<IResponse> RateMission(IMissionService.RateMissionRequest request)
		{
			var response = new Response();

			async Task notNullRequest()
			{
				Expire();

				String name = request.Name?.ToLower()??String.Empty;
				MissionEntity mission = Connection.GetSingle<MissionEntity>(m=>m.Name.ToLower().Equals(name));

				Boolean validRequest()
				{
					return request.TimeTaken.HasValue || !String.IsNullOrEmpty(request.Review);
				}

				void rate()
				{
					TimeSpan time = request.TimeTaken ?? TimeSpan.FromTicks((long)mission.Ratings.Average(r => r.TimeTaken.Ticks));
					var rating = new MissionRatingEntity(request.Review, time, Session.User);
					Connection.Insert(rating);
					mission.Ratings.Add(rating);
					Connection.Update(mission);
					Connection.SaveChanges();

					MissionUpdated.Invoke(Session, mission, mission.CloneAsT());
				}

				await FirstValidateAuthenticated()
					.NextNullCheck(mission,
						response.Validation.GetField(nameof(request.Name)),
						ValidationCode.NotFound)
					.NextCompound(validRequest,
						response.Validation.GetField(nameof(request)),
						ValidationCode.Invalid.WithMessage("Time and/or Review is required."))
					.SetOnCriterionMet(rate)
					.Evaluate(response);
			}

			await FirstRequestNullCheck(request, response)
				.SetOnCriterionMet(notNullRequest)
				.CatchAll(ValidationField.Request)
				.Evaluate(response);

			return response;
		}

		public async Task<IResponse> RemoveMission(IMissionService.RemoveMissionRequest request)
		{
			var response = new Response();

			async Task requestNotNull(){
				Expire();

				MissionEntity mission = Connection.GetSingle<MissionEntity>(m => m.Name.Equals(request.Name));

				Boolean isCreator()
				{
					return Session.User.Id == mission.Creator!.Id;
				}

				void successAction()
				{
					Connection.DeleteAndSaveChanges(mission);
					MissionRemoved.Invoke(mission);
				}

				await FirstValidateAuthenticated()
					.NextNullCheck(mission,
						response.Validation.GetField(nameof(request.Name)),
						ValidationCode.NotFound)
					.NextCompound(isCreator,
						response.Validation.GetField(nameof(request.Name)),
						ValidationCode.Unauthorized)
					.SetOnCriterionMet(successAction)
					.Evaluate(response);
			}

			await FirstRequestNullCheck(request, response)
				.SetOnCriterionMet(requestNotNull)
				.CatchAll(ValidationField.Request)
				.Evaluate(response);

			return response;
		}

		private void Expire()
		{
			GetService<PBApplication.Services.Abstractions.IEventfulManageExpirantsService>().RunOverExpiredExpirants<MissionEntity>(remove);

			void remove(MissionEntity m)
			{
				Connection.DeleteAndSaveChanges(m);
				MissionRemoved.Invoke(m);
			}
		}
	}
}
