using PBApplication.Events.Notifiers.Abstractions;
using PBApplication.Events.Publishing.Abstractions;
using UnityShuffle.Data;
using UnityShuffle.Services.Abstractions;

namespace UnityShuffle.Events
{
	public sealed class MissionEntityNotifier: DataSubscriptionEventNotifier<MissionEntity>
	{
		public MissionEntityNotifier(MissionEntity data, IContextualEventSubscriptionManager subscriptionManager) : base(data, subscriptionManager)
		{
			subscriptionManager.Subscribe(new(nameof(IMissionService.MissionDeleted), data.HubId), () => SetData(null));
			subscriptionManager.Subscribe<MissionEntity>(new(nameof(IMissionService.MissionRated), data.HubId), SetData);
		}
	}
}
