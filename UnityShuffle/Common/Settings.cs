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
			Branches = Initializer.Instance.Aspects;
			MissionLifespan = Initializer.Instance.MissionLifespan;
			MissionCreatedHubId = Initializer.Instance.MissionCreatedHubId;
			MaxMissionNameChars = Initializer.Instance.MaxMissionNameChars;
			MinMissionNameChars = Initializer.Instance.MinMissionNameChars;
			MaxMissionDescriptionChars = Initializer.Instance.MaxMissionDescriptionChars;
			MinMissionDescriptionChars = Initializer.Instance.MinMissionDescriptionChars;
		}
		public static readonly IEnumerable<String> Branches;
		public static readonly IEnumerable<String> Locations;
		public static readonly TimeSpan MissionLifespan;
		public static readonly Guid MissionCreatedHubId;
		public static readonly Int32 MaxMissionNameChars;
		public static readonly Int32 MinMissionNameChars;
		public static readonly Int32 MaxMissionDescriptionChars;
		public static readonly Int32 MinMissionDescriptionChars;
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
		public IEnumerable<String> Aspects { get; set; } = new[] { "Forschung", "Raumüberlegenheit", "Marinecorps", "Profit", "Search And Rescue", "Subradar" };
		public TimeSpan MissionLifespan { get; set; } = TimeSpan.FromDays(28);
		public Guid MissionCreatedHubId { get; set; } = Guid.NewGuid();
		public Int32 MaxMissionNameChars { get; set; } = 100;
		public Int32 MinMissionNameChars { get; set; } = 4;
		public Int32 MaxMissionDescriptionChars { get; set; } = 1000;
		public Int32 MinMissionDescriptionChars { get; set; } = 10;
	}
}
