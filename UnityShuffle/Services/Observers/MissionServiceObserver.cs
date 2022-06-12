using PBApplication.Events.Publishing.Abstractions;
using UnityShuffle.Services.Abstractions;

namespace UnityShuffle.Services.Observers
{
	public sealed class MissionServiceObserver : PBApplication.Events.ServiceObservers.ServiceObserver<IMissionService>
	{
		public MissionServiceObserver(IEventPublisher publisher) : base(publisher)
		{
		}

		public override void Observe(IMissionService service)
		{
			service.MissionRated += args => Send(nameof(IMissionService.MissionRated), args);
			service.MissionCreated += args => Send(nameof(IMissionService.MissionCreated), args);
			service.MissionDeleted += args => Send(nameof(IMissionService.MissionDeleted), args);
		}
	}
}
