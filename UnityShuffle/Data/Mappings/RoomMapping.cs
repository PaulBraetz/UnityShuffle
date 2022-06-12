using PBData.Mapping;

namespace UnityShuffle.Data.Mappings
{
	public class RoomMapping : ExpiringMappingBase<RoomEntity>
	{
		public RoomMapping()
		{
			Map(m=>m.Name).Length(PBData.Configuration.Settings.MaxStringFieldLength);
			HasManyToMany(m => m.Deck);
			HasManyToMany(m => m.Drawn);
		}
	}
}
