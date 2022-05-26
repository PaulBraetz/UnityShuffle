using PBApplication.Events;
using PBApplication.Responses.Abstractions;
using PBApplication.Services.Abstractions;
using UnityShuffle.Data;

namespace UnityShuffle.Services.Abstractions
{
	public interface IMissionService:IService
	{
		sealed class GetMissionRequest
		{
			public String Name { get; set; } = String.Empty;
			public String Location { get; set; } = String.Empty;
			public String Description { get; set; } = String.Empty;
			public IEnumerable<String> Aspects { get; set; } = Array.Empty<String>();
			public TimeSpan? MaxTime { get; set; }
		}

		Task<IResponse<MissionEntity>> GetMission(GetMissionRequest request);

		//Recipients: Settings.MissionAddedHubId
		//Payload: new mission
		event ServiceEventHandler<ServiceEventArgs<MissionEntity>> MissionAdded;
		sealed class AddMissionRequest
		{
			public String Name { get; set; } = String.Empty;
			public String Description { get; set; } = String.Empty;
			public String Location { get; set; } = String.Empty;
			public IEnumerable<String> Aspects { get; set; } = Array.Empty<String>();
		}
		Task<IResponse> AddMission(AddMissionRequest request);

		//Recipient: removed mission
		event ServiceEventHandler<ServiceEventArgs> MissionRemoved;
		sealed class RemoveMissionRequest
		{
			public String Name { get; set; } = String.Empty;
		}
		Task<IResponse> RemoveMission(RemoveMissionRequest request);

		//Recipient: updated mission
		//Payload: updated mission
		event ServiceEventHandler<ServiceEventArgs<MissionEntity>> EventUpdated;
		sealed class RateMissionRequest
		{
			public String Name { get; set; } = String.Empty;
			public String Review { get; set; } = String.Empty;
			public TimeSpan? Time { get; set; }
		}
		Task<IResponse> RateMission(RateMissionRequest request);
	}
}
