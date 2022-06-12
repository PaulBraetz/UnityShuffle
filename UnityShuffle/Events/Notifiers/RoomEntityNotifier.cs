using PBApplication.Events.Notifiers.Abstractions;
using PBApplication.Events.Publishing.Abstractions;
using UnityShuffle.Data;
using UnityShuffle.Services.Abstractions;

namespace UnityShuffle.Events
{
	public sealed class RoomEntityNotifier : OverwritableDataSubscriptionEventNotifier<IMissionService.RoomDto>
	{
		public RoomEntityNotifier(IMissionService.RoomDto overwritable, IContextualEventSubscriptionManager subscriptionManager) : base(overwritable, subscriptionManager)
		{
			subscriptionManager.Subscribe(new(nameof(IMissionService.RoomClosed), overwritable.HubId), () => SetData(null));
			subscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionAdded), overwritable.HubId), OverwriteData);
			subscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionRemoved), overwritable.HubId), OverwriteData);
			subscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.RoomShuffled), overwritable.HubId), OverwriteData);
			subscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.RoomJoined), overwritable.HubId), OverwriteData);
			subscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionDrawn), overwritable.HubId), OverwriteData);
			subscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionSkipped), overwritable.HubId), OverwriteData);
		}
	}
}
