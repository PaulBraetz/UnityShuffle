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

		//Recipient: Expired Room
		event ServiceEventHandler<ServiceEventArgs>? RoomExpired;

		sealed class RoomDto : EncryptableBase<Guid>, IOverwritable<RoomDto>
		{
			public RoomDto(RoomEntity room)
			{
				JoinKey = room.JoinKey;
				Next = room.Next;
				deck = room.Deck.ToList();
				Top = room.Top;
				drawn = room.Drawn.ToList();
			}
			public String JoinKey { get; private set; }
			public MissionEntity? Next { get; private set; }
			private ICollection<MissionEntity> deck;
			public IEnumerable<MissionEntity> Deck => deck;
			public MissionEntity? Top { get; private set; }
			private ICollection<MissionEntity> drawn;
			public IEnumerable<MissionEntity> Drawn => drawn;

			public RoomDto Overwrite(RoomDto with)
			{
				JoinKey = with.JoinKey;
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
		Task<IEncryptableResponse<RoomDto>> GetRoom();

		event ServiceEventHandler<ServiceEventArgs<RoomDto>> MissionAdded;
		Task<IResponse> Add(String missionName);

		event ServiceEventHandler<ServiceEventArgs<MissionEntity>> MissionRemoved;
		Task<IResponse> Remove(String missionName);

		event ServiceEventHandler<ServiceEventArgs<MissionEntity>> RoomShuffled;
		Task<IResponse> Shuffle();

		event ServiceEventHandler<ServiceEventArgs<MissionEntity>> RoomJoined;
		Task<IResponse> Join();

		//Recipient: Affected Room
		//Payload: Drawn Mission
		event ServiceEventHandler<ServiceEventArgs<MissionEntity?>>? MissionDrawn;
		Task<IResponse> Draw();

		//Recipient: Affected Room
		//Payload: Drawn Mission
		event ServiceEventHandler<ServiceEventArgs<MissionEntity>>? MissionSkipped;
		Task<IResponse> Skip();

		Task<IEncryptableResponse<RoomDto>> GetRoom(String joinKey);
	}
}
