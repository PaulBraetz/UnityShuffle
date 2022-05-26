using PBData.Mapping;

namespace UnityShuffle.Data.Mappings
{
	public class MissionRatingMapping : MappingBase<MissionRatingEntity>
	{
		public MissionRatingMapping()
		{
			Map(m=>m.Review).Length(PBData.Configuration.Settings.MaxStringFieldLength);
			Map(m => m.Time);
			References(m=>m.Creator);
		}
	}
}
