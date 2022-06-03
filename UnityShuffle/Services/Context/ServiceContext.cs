using PBApplication.Events.Publishing.Abstractions;
using PBApplication.Events.ServiceObservers.Abstractions;
using UnityShuffle.Services.Abstractions;
using UnityShuffle.Services.Observers;

namespace UnityShuffle.Services.Context
{
	public sealed class ServiceContext : PBServer.Context.ObservingServiceContext
	{
		public ServiceContext(IHttpContextAccessor httpContextAccessor, IEventPublisher publisher) : base(httpContextAccessor, publisher)
		{
			RegisterTypeToServices<IMissionService, MissionService>(this);

			RegisterTypeToObservers<IServiceObserver<IMissionService>, MissionServiceObserver, IMissionService>(publisher);
		}
	}
}
