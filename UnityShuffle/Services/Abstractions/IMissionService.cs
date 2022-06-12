using PBApplication.Events;
using PBApplication.Requests.Abstractions;
using PBApplication.Responses.Abstractions;
using PBApplication.Services.Abstractions;
using PBCommon;
using PBCommon.Encryption;
using PBCommon.Encryption.Abstractions;
using PBData.Extensions;
using UnityShuffle.Data;

namespace UnityShuffle.Services.Abstractions
{
	public interface IMissionService : IService
	{
		sealed class GetMissionRequest
		{
			public String Name { get; set; } = String.Empty;
			public String Location { get; set; } = String.Empty;
			public String Description { get; set; } = String.Empty;
			public IEnumerable<String> Aspects { get; set; } = Array.Empty<String>();
			public TimeSpan? MaxTime { get; set; }
		}
		Task<IGetPaginatedEncryptableResponse<MissionEntity>> GetMissions(IGetPaginatedRequest<GetMissionRequest> request);

		//Recipients: Settings.MissionAddedHubId
		//Payload: new mission
		event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionCreated;
		sealed class CreateMissionRequest
		{
			public String Name { get; set; } = String.Empty;
			public String Description { get; set; } = String.Empty;
			public String Location { get; set; } = String.Empty;
			public IEnumerable<String> Branches { get; set; } = Array.Empty<String>();
		}
		Task<IResponse> CreateMission(CreateMissionRequest request);

		//Recipient: deleted mission
		event ServiceEventHandler<ServiceEventArgs>? MissionDeleted;
		sealed class DeleteMissionRequest
		{
			public String Name { get; set; } = String.Empty;
		}
		Task<IResponse> DeleteMission(DeleteMissionRequest request);

		//Recipient: updated mission
		//Payload: updated mission
		event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionUpdated;
		sealed class RateMissionRequest
		{
			public String Name { get; set; } = String.Empty;
			public String Review { get; set; } = String.Empty;
			public TimeSpan? TimeTaken { get; set; }
		}
		Task<IResponse> RateMission(RateMissionRequest request);

		sealed class RoomDto : EncryptableBase<Guid>, IOverwritable<RoomDto>
		{
			public RoomDto(RoomEntity room)
			{
				Name = room.Name;
				Next = room.Next;
				deck = room.Deck.ToList();
				Top = room.Top;
				drawn = room.Drawn.ToList();
			}
			public String Name { get; private set; }
			public MissionEntity? Next { get; private set; }
			private ICollection<MissionEntity> deck;
			public IEnumerable<MissionEntity> Deck => deck;
			public MissionEntity? Top { get; private set; }
			private ICollection<MissionEntity> drawn;
			public IEnumerable<MissionEntity> Drawn => drawn;

			public RoomDto Overwrite(RoomDto with)
			{
				Name = with.Name;
				Next = with.Next;
				deck = with.deck;
				Top = with.Top;
				drawn = with.drawn;
				return this;
			}

			protected override async Task DecryptSelf(IDecryptor<Guid> decryptor)
			{
				await Task.WhenAll(Deck.SafeDecrypt(decryptor),
					Drawn.SafeDecrypt(decryptor));
			}
			protected override async Task EncryptSelf(IEncryptor<Guid> encryptor)
			{
				await Task.WhenAll(Deck.SafeEncrypt(encryptor),
					Drawn.SafeEncrypt(encryptor));
			}
		}

		sealed class CreateRoomRequest
		{
			public String? RoomName { get; set; }
		}
		Task<IResponse> CreateRoom(CreateRoomRequest request);

		//Recipient: Closed Room
		event ServiceEventHandler<ServiceEventArgs>? RoomClosed;
		Task<IResponse> CloseRoom(String roomName);

		//Recipient: Affected Room
		//Payload: Affected Room
		event ServiceEventHandler<ServiceEventArgs<RoomDto>>? MissionAdded;
		Task<IResponse> Add(String roomName, String missionName);

		//Recipient: Affected Room
		//Payload: Removed Mission
		event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionRemoved;
		Task<IResponse> Remove(String roomName, String missionName);

		//Recipient: Affected Room
		//Payload: Room
		event ServiceEventHandler<ServiceEventArgs<RoomDto>>? RoomShuffled;
		Task<IResponse> Shuffle(String roomName);

		//Recipient: Affected Room
		//Payload: Room
		event ServiceEventHandler<ServiceEventArgs<RoomDto>>? RoomJoined;
		Task<IResponse> Join(String roomName);

		//Recipient: Affected Room
		//Payload: Affected Room
		event ServiceEventHandler<ServiceEventArgs<RoomDto>>? MissionDrawn;
		Task<IResponse> Draw(String roomName);

		//Recipient: Affected Room
		//Payload: Affected Room
		event ServiceEventHandler<ServiceEventArgs<RoomDto>>? MissionSkipped;
		Task<IResponse> Skip(String roomName);

		Task<IEncryptableResponse<RoomDto>> GetRoom(String name);
	}
}
