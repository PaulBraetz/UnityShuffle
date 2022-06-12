
using PBApplication.Context.Abstractions;
using PBApplication.Events;
using PBApplication.Extensions;
using PBApplication.Requests.Abstractions;
using PBApplication.Responses;
using PBApplication.Responses.Abstractions;
using PBApplication.Services;
using PBApplication.Services.Abstractions;
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

		public event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionCreated;
		public event ServiceEventHandler<ServiceEventArgs>? MissionDeleted;
		public event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionRated;
		public event ServiceEventHandler<ServiceEventArgs>? RoomClosed;
		public event ServiceEventHandler<ServiceEventArgs<IMissionService.RoomDto>>? MissionAdded;
		public event ServiceEventHandler<ServiceEventArgs<IMissionService.RoomDto>>? MissionRemoved;
		public event ServiceEventHandler<ServiceEventArgs<IMissionService.RoomDto>>? RoomShuffled;
		public event ServiceEventHandler<ServiceEventArgs<IMissionService.RoomDto>>? RoomJoined;
		public event ServiceEventHandler<ServiceEventArgs<IMissionService.RoomDto>>? MissionDrawn;
		public event ServiceEventHandler<ServiceEventArgs<IMissionService.RoomDto>>? MissionSkipped;

		private MissionEntity? GetMissionEntity(String? name)
		{
			ExpireMissions();
			return Connection.GetSingle<MissionEntity>(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
		}

		public async Task<IResponse> CreateMission(IMissionService.CreateMissionRequest request)
		{
			var response = new Response();

			async Task notNullRequest()
			{
				Boolean validName()
				{
					return request.Name.IsWithinLimitsAndAlphanumeric(Common.Settings.MinMissionNameChars, Common.Settings.MaxMissionNameChars);
				}

				Boolean validLocation()
				{
					return Common.Settings.Locations.Contains(request.Location);
				}
				Boolean validAspects()
				{
					return request.Branches.All(a => Common.Settings.Branches.Contains(a));
				}
				Boolean validDescription()
				{
					return request.Description.IsWithinLimitsAndAlphanumeric(Common.Settings.MinMissionNameChars, Common.Settings.MaxMissionNameChars);
				}

				MissionEntity? duplicate = GetMissionEntity(request.Name);

				void successAction()
				{
					var newMission = new MissionEntity(request.Name,
														request.Description,
														request.Location,
														Session.User,
														Common.Settings.MissionLifespan)
					{
						Branches = request.Branches
					};
					newMission.RefreshNow();

					Connection.Insert(newMission);
					Connection.SaveChanges();

					MissionCreated.Invoke(Session, Common.Settings.MissionCreatedHubId, newMission.CloneAsT());
				}

				await FirstCompound(validName,
						response.EnsureField(nameof(request.Name)),
						ValidationCode.Invalid.SerializeMessage("Please make sure the name provided is alphanumeric and between {0} and {1} characters.", Common.Settings.MinMissionNameChars, Common.Settings.MaxMissionNameChars))
					.NextCompound(validLocation,
						response.EnsureField(nameof(request.Location)),
						ValidationCode.Invalid.SerializeMessage("Please make sure the location provided is one of the following: {0}.", String.Join(", ", Common.Settings.Locations)))
					.NextCompound(validAspects,
						response.EnsureField(nameof(request.Branches)),
						ValidationCode.Invalid.SerializeMessage("Please make sure the branches provided are valid. Valid branches are: {0}.", String.Join(", ", Common.Settings.Branches)))
					.NextCompound(validDescription,
						response.EnsureField(nameof(request.Description)),
						ValidationCode.Invalid.SerializeMessage("Please make sure the description provided is alphanumeric and between {0} and {1} characters.", Common.Settings.MinMissionDescriptionChars, Common.Settings.MaxMissionDescriptionChars))
					.NextNullCheck(duplicate,
						response.EnsureField(nameof(request.Name)),
						ValidationCode.Duplicate.WithMessage("This name is already taken."))
					.InvertCriterion()
					.SetOnCriterionMet(successAction)
					.Evaluate(response);
			}

			await FirstRequestNullCheck(request, response)
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
				ExpireMissions();

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
						query = query.Where(m => m.Branches.Contains(lowerCaseAspect));
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
				MissionEntity? mission = GetMissionEntity(request.Name);

				Boolean validRequest()
				{
					return request.TimeTaken.HasValue || !String.IsNullOrEmpty(request.Review);
				}

				void rate()
				{
					TimeSpan time = request.TimeTaken ?? TimeSpan.FromTicks((long)mission.Ratings.Average(r => r.TimeTaken.Ticks));
					var rating = new MissionRatingEntity(request.Review, time, Session.User);
					Connection.Insert(rating);
					mission!.Ratings.Add(rating);
					Connection.Update(mission);
					Connection.SaveChanges();

					MissionRated.Invoke(Session, mission, mission.CloneAsT());
				}

				await FirstValidateAuthenticated()
					.NextNullCheck(mission,
						response.EnsureField(nameof(request.Name)),
						ValidationCode.NotFound)
					.NextCompound(validRequest,
						response.EnsureField(nameof(request)),
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

		public async Task<IResponse> DeleteMission(IMissionService.DeleteMissionRequest request)
		{
			var response = new Response();

			async Task requestNotNull()
			{
				MissionEntity? mission = GetMissionEntity(request.Name);

				Boolean isCreator()
				{
					return Session.User.Id == mission!.Creator!.Id;
				}

				void successAction()
				{
					Connection.DeleteAndSaveChanges(mission);
					MissionDeleted.Invoke(mission);
				}

				await FirstValidateAuthenticated()
					.NextNullCheck(mission,
						response.EnsureField(nameof(request.Name)),
						ValidationCode.NotFound)
					.NextCompound(isCreator,
						response.EnsureField(nameof(request.Name)),
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

		private void ExpireMissions()
		{
			GetService<IEventfulManageExpirantsService>().RunOverExpiredExpirants<MissionEntity>(remove);

			void remove(MissionEntity m)
			{
				Connection.DeleteAndSaveChanges(m);
				MissionDeleted.Invoke(m);
			}
		}

		private RoomEntity? GetAttachedRoomEntity(String? roomName)
		{
			return Session.GetAttached<RoomEntity>(Connection).SingleOrDefault(r => r.Name.Equals(roomName, StringComparison.InvariantCultureIgnoreCase));
		}

		private async Task<IResponse> RoomAction(String roomName, Action<RoomEntity> action)
		{
			var response = new Response();

			var room = GetAttachedRoomEntity(roomName);

			void @do()
			{
				action.Invoke(room!);
			}

			await FirstNullCheck(room,
					response.EnsureField(nameof(roomName)),
					ValidationCode.NotFound)
				.SetOnCriterionMet(@do)
				.CatchAll(ValidationField.Request)
				.Evaluate(response);

			return response;
		}

		private async Task<IResponse> RoomFunc(String roomName, Func<RoomEntity, IResponse, Task> func)
		{
			var response = new Response();

			var room = GetAttachedRoomEntity(roomName);

			Task @do()
			{
				return func.Invoke(room!, response);
			}

			await FirstNullCheck(room,
					response.EnsureField(nameof(roomName)),
					ValidationCode.NotFound)
				.SetOnCriterionMet(@do)
				.CatchAll(ValidationField.Request)
				.Evaluate(response);

			return response;
		}

		public Task<IResponse> CloseRoom(String roomName)
		{
			void close(RoomEntity room)
			{
				GetService<IEventfulUserService>().DetachFromSession(room);
				Connection.Delete(room);
				Connection.SaveChanges();

				RoomClosed.Invoke(room);
			}
			return RoomAction(roomName, close);
		}

		public Task<IResponse> Add(String roomName, String missionName)
		{
			async Task add(RoomEntity room, IResponse response)
			{
				MissionEntity? mission = GetMissionEntity(missionName);

				void add()
				{
					room.Add(mission!);
					Connection.Update(room);
					Connection.SaveChanges();

					MissionAdded.Invoke(Session, room, new IMissionService.RoomDto(room.CloneAsT()));
				}

				await FirstNullCheck(mission,
						response.EnsureField(nameof(missionName)),
						ValidationCode.NotFound)
					.SetOnCriterionMet(add)
					.Evaluate(response);
			}
			return RoomFunc(roomName, add);
		}

		public Task<IResponse> Remove(String roomName, String missionName)
		{
			async Task remove(RoomEntity room, IResponse response)
			{
				MissionEntity? mission = GetMissionEntity(missionName);

				void remove()
				{
					room.Remove(mission!);
					Connection.Update(room);
					Connection.SaveChanges();

					MissionRemoved.Invoke(Session, room, new IMissionService.RoomDto(room.CloneAsT()));
				}

				await FirstNullCheck(mission,
						response.EnsureField(nameof(missionName)),
						ValidationCode.NotFound)
					.SetOnCriterionMet(remove)
					.Evaluate(response);
			}
			return RoomFunc(roomName, remove);
		}

		public Task<IResponse> Shuffle(String roomName)
		{
			void shuffle(RoomEntity room)
			{
				room!.Shuffle();
				Connection.Update(room);
				Connection.SaveChanges();

				RoomShuffled.Invoke(Session, room, new IMissionService.RoomDto(room.CloneAsT()));
			}

			return RoomAction(roomName, shuffle);
		}

		public Task<IResponse> Join(String roomName)
		{
			void join(RoomEntity room)
			{
				room!.Join();
				Connection.Update(room);
				Connection.SaveChanges();

				RoomJoined.Invoke(Session, room, new IMissionService.RoomDto(room.CloneAsT()));
			}

			return RoomAction(roomName, join);
		}

		public Task<IResponse> Draw(String roomName)
		{
			void draw(RoomEntity room)
			{
				room!.Draw();
				Connection.Update(room);
				Connection.SaveChanges();

				MissionDrawn.Invoke(Session, room, new IMissionService.RoomDto(room.CloneAsT()));
			}

			return RoomAction(roomName, draw);
		}

		public Task<IResponse> Skip(String roomName)
		{
			void skip(RoomEntity room)
			{
				room!.Skip();
				Connection.Update(room);
				Connection.SaveChanges();

				MissionSkipped.Invoke(Session, room, new IMissionService.RoomDto(room.CloneAsT()));
			}

			return RoomAction(roomName, skip);
		}

		private RoomEntity? GetRoomEntity(String? name)
		{
			return Connection.GetSingle<RoomEntity>(r => r.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
		}

		public async Task<IEncryptableResponse<IMissionService.RoomDto>> GetRoom(String name)
		{
			var response = new EncryptableResponse<IMissionService.RoomDto>();

			var room = GetRoomEntity(name);

			void roomNotNull()
			{
				response.Overwrite(new IMissionService.RoomDto(room!.CloneAsT()));
			}

			await FirstNullCheck(room,
					response.EnsureField(nameof(name)),
					ValidationCode.NotFound)
				.SetOnCriterionMet(roomNotNull)
				.CatchAll(ValidationField.Request)
				.Evaluate(response);

			return response;
		}

		public async Task<IResponse> CreateRoom(IMissionService.CreateRoomRequest request)
		{
			var response = new Response();

			async Task notNullRequest()
			{
				String? roomName = request.RoomName;

				void create()
				{
					var newRoom = new RoomEntity(roomName);
					Connection.Insert(newRoom);
					GetService<IEventfulUserService>().AttachToSession(newRoom);
				}
				RoomEntity? getDuplicate()
				{
					return GetRoomEntity(roomName);
				}

				if (roomName == null)
				{
					var bytes = new Byte[8];
					String getName()
					{
						Random.Shared.NextBytes(bytes);
						return Convert.ToBase64String(bytes);
					}
					do
					{
						roomName = getName();
					} while (getDuplicate() != null);

					create();
				}
				else
				{
					var duplicate = getDuplicate();

					await FirstNullCheck(duplicate,
							response.EnsureField(nameof(request.RoomName)),
							ValidationCode.Duplicate)
						.SetOnCriterionMet(create)
						.Evaluate(response);
				}
			}

			await FirstRequestNullCheck(request, response)
				.SetOnCriterionMet(notNullRequest)
				.CatchAll(response.EnsureField(nameof(request)))
				.Evaluate(response);

			return response;
		}
	}
}
