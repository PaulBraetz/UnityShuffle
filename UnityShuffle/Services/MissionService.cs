
using PBApplication.Context.Abstractions;
using PBApplication.Events;
using PBApplication.Responses;
using PBApplication.Responses.Abstractions;
using PBApplication.Services;
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
		public event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? EventUpdated;

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

		public async Task<IResponse<MissionEntity>> GetMission(IMissionService.GetMissionRequest request)
		{
			var response = new Response<MissionEntity>();

			async Task requestNotNull()
			{
				Expire();

				IEnumerable<MissionEntity> query = Connection.Query<MissionEntity>();

				if (!String.IsNullOrWhiteSpace(request.Name))
				{
					String name = request.Name.ToLower();
					query = query.Where(m=>m.Name.ToLower().Contains(name));
				}
				if (!String.IsNullOrWhiteSpace(request.Location))
				{
					String location = request.Location.ToLower();
					query = query.Where(m => m.Location.ToLower().Equals(location));
				}
				if (!String.IsNullOrWhiteSpace(request.Description))
				{
					String description = request.Description.ToLower();
					query = query.Where(m => m.Description.ToLower().Equals(description));
				}
				if (request.Aspects?.Any() ?? false)
				{
					String lowerCaseAspect = String.Empty;
					foreach(var aspect in request.Aspects)
					{
						lowerCaseAspect = aspect.ToLower();
						query = query.Where(m=>m.Aspects.Contains(lowerCaseAspect));
					}
				}
				if (request.MaxTime.HasValue)
				{
					Int64 max = request.MaxTime.Value.Ticks;
					query = query.Where(m=>m.Ratings.Average(r=>r.TimeTaken.Ticks) <= max);
				}

				query = query.ToList();

				int last = query.Count()-1;
				MissionEntity random = query.ElementAt(Random.Shared.Next(0, last));

				void successAction()
				{
					response.Overwrite(random);
				}

				await FirstNullCheck(random,
						response.Validation.GetField(nameof(request)),
						ValidationCode.NotFound)
					.SetOnCriterionMet(successAction)
					.Evaluate(response);
			}

			await FirstRequestNullCheck(request, response)
				.SetOnCriterionMet(requestNotNull)
				.CatchAll(ValidationField.Request)
				.Evaluate(response);

			return response;
		}

		public async Task<IResponse> RateMission(IMissionService.RateMissionRequest request)
		{
			var response = new Response();

			async Task notNullRequest()
			{
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

					EventUpdated.Invoke(Session, mission, mission.CloneAsT());
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
