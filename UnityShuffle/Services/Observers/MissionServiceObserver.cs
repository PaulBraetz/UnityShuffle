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
			service.MissionUpdated += args => Send(nameof(IMissionService.MissionUpdated), args);
			service.MissionAdded += args => Send(nameof(IMissionService.MissionAdded), args);
			service.MissionRemoved += args => Send(nameof(IMissionService.MissionRemoved), args);
		}
	}
}
