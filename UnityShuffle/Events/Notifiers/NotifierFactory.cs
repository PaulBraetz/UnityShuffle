using PBApplication.Events.Notifiers;
using PBApplication.Events.Publishing.Abstractions;
using UnityShuffle.Data;

namespace UnityShuffle.Events
{
	public sealed class NotifierFactory : ChangeNotifierFactory
	{
		public NotifierFactory(IContextualEventSubscriptionManagerFactory factory) : base(factory)
		{
			base.RegisterNotifier<MissionEntityNotifier, MissionEntity>(factory.Invoke());
			base.RegisterNotifier<RoomEntityNotifier, RoomEntity>(factory.Invoke());
		}
	}
}
