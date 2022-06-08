using PBApplication.Events.Notifiers;
using PBApplication.Events.Notifiers.Abstractions;
using PBApplication.Events.Publishing.Abstractions;
using UnityShuffle.Data;
using UnityShuffle.Services.Abstractions;

namespace UnityShuffle.Events
{
	public sealed class RoomEntityNotifier : OverwritableDataSubscriptionEventNotifier<IMissionService.RoomDto, RoomEntity>
	{
		public RoomEntityNotifier(IMissionService.RoomDto overwritable, IContextualEventSubscriptionManager subscriptionManager) : base(overwritable, subscriptionManager)
		{
			subscriptionManager.Subscribe<MissionEntity>(new(nameof(IMissionService.)))
		}
	}
	public sealed class MissionEntityNotifier : DataSubscriptionEventNotifier<MissionEntity>
	{
		public MissionEntityNotifier(MissionEntity data, IContextualEventSubscriptionManager subscriptionManager) : base(data, subscriptionManager)
		{
			subscriptionManager.Subscribe(new(nameof(IMissionService.MissionDeleted), data.HubId), () => SetData(null));
			subscriptionManager.Subscribe<MissionEntity>(new(nameof(IMissionService.MissionUpdated), data.HubId), SetData);
		}
	}
	public sealed class NotifierFactory : ChangeNotifierFactory
	{
		public NotifierFactory(IContextualEventSubscriptionManagerFactory factory) : base(factory)
		{
			base.RegisterNotifier<MissionEntityNotifier, MissionEntity>(factory.Invoke());
		}
	}
}
