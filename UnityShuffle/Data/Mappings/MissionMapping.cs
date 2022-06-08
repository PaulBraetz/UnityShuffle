using PBData.Mapping;

namespace UnityShuffle.Data.Mappings
{
	public class MissionMapping : ExpiringMappingBase<MissionEntity>
	{
		public MissionMapping()
		{
			Map(m => m.Location).Length(PBData.Configuration.Settings.MaxStringFieldLength);
			Map(m=>m.Name).Length(PBData.Configuration.Settings.MaxStringFieldLength);
			Map(m => m.Description).Length(PBData.Configuration.Settings.MaxStringFieldLength);
			HasMany(m=>m.Branches).Table($"{nameof(MissionEntity)}ToAspects").Element("AspectName");
			References(m=>m.Creator);
			HasMany(m=>m.Ratings);
		}
	}
}
