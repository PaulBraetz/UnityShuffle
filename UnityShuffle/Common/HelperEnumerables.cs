using PBFrontend.UI.Input;

namespace UnityShuffle.Common
{
	public static class HelperEnumerables
	{
		public static readonly IEnumerable<SelectInput<String>.OptionModel> LocationOptions = Settings.Locations
			.Select(l => new SelectInput<string>.OptionModel(l, l))
			.ToList()
			.AsReadOnly();

		public static readonly IEnumerable<SelectInput<String>.OptionModel> BranchOptions = Settings.Branches
			.Select(l => new SelectInput<string>.OptionModel(l, l))
			.ToList()
			.AsReadOnly();
	}
}
