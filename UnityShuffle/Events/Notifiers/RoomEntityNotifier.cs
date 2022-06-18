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
			SubscriptionManager.Subscribe(new(nameof(IMissionService.RoomClosed), overwritable.HubId), () => SetData(null));
			SubscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionAdded), overwritable.HubId), OverwriteData);
			SubscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionRemoved), overwritable.HubId), OverwriteData);
			SubscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.RoomShuffled), overwritable.HubId), OverwriteData);
			SubscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.RoomJoined), overwritable.HubId), OverwriteData);
			SubscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionDrawn), overwritable.HubId), OverwriteData);
			SubscriptionManager.Subscribe<IMissionService.RoomDto>(new(nameof(IMissionService.MissionSkipped), overwritable.HubId), OverwriteData);
		}
	}
}
