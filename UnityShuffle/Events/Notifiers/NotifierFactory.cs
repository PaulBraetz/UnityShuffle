using PBApplication.Events.Notifiers;
using PBApplication.Events.Publishing.Abstractions;
using UnityShuffle.Data;
using UnityShuffle.Services.Abstractions;

namespace UnityShuffle.Events
{
	public sealed class NotifierFactory : ChangeNotifierFactory
	{
		public NotifierFactory(IContextualEventSubscriptionManagerFactory factory) : base(factory)
		{
			base.RegisterNotifier<MissionEntityNotifier, MissionEntity>(factory.Invoke());
			base.RegisterNotifier<RoomEntityNotifier, IMissionService.RoomDto>(factory.Invoke());
		}
	}
}
