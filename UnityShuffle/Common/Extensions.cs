using System.Text.RegularExpressions;

namespace UnityShuffle.Common
{
	public static class Extensions
	{
		public static Boolean IsValidMissionName(this String? strToCheck)
		{
			var pattern = $@"^[a-zA-Z0-9][a-zA-Z0-9\s]{{{Settings.MinMissionNameChars-2},{Settings.MaxMissionNameChars-2}}}[a-zA-Z0-9]$";
			return strToCheck != null && Regex.IsMatch(strToCheck, pattern);
		}
		public static Boolean IsValidMissionDescription(this String? strToCheck)
		{
			var pattern = $@"^[a-zA-Z0-9][a-zA-Z0-9\s]{{{Settings.MinMissionDescriptionChars - 2},{Settings.MaxMissionDescriptionChars - 2}}}[a-zA-Z0-9]$";
			return strToCheck != null && Regex.IsMatch(strToCheck, pattern);
		}
	}
}
