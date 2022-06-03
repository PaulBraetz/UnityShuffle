using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnityShuffle.Common
{
	public static class Settings
	{
		public static readonly String Version = Assembly.GetAssembly(typeof(Settings))?.GetName().Version?.ToString()??String.Empty;
		static Settings()
		{
			//use static readonly fields
			//Initialize fields using Initializer.Instance fields here
			Locations = Initializer.Instance.Locations;
			MissionLifespan = Initializer.Instance.MissionLifespan;
			MissionAddedHubId = Initializer.Instance.MissionAddedHubId;
		}
		public static readonly IEnumerable<String> Locations;
		public static readonly TimeSpan MissionLifespan;
		public static readonly Guid MissionAddedHubId; 
	}
	public sealed class Initializer
	{
		private Initializer() { }
		internal static readonly Initializer Instance = new();
		internal static void ConfigureSettingsInitializer(Action<Initializer> configure)
		{
			configure.Invoke(Initializer.Instance);
			RuntimeHelpers.RunClassConstructor(typeof(Settings).TypeHandle);
		}
		//Add Settings fields here, make them read/write as necessary
		public IEnumerable<String> Locations { get; set; } = new[] {"Crusader","Hurston", "ArcCorp", "MicroTech" };
		public TimeSpan MissionLifespan { get; set; } = TimeSpan.FromDays(28);
		public Guid MissionAddedHubId { get; set; } = Guid.NewGuid();
	}
}
