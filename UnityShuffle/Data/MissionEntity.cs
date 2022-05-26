using PBData.Abstractions;
using PBData.Entities;
using PBData.Extensions;

namespace UnityShuffle.Data
{
	public class MissionEntity:ExpiringEntityBase, IHasCreator<UserEntity>
	{
		public MissionEntity() { }
		public MissionEntity(String name, String description, String location, UserEntity creator, TimeSpan lifeSpan, params String[] aspects) : base(lifeSpan, true, false)
		{
			Name = name;
			Description = description;
			Location = location;
			Aspects = aspects;
			Ratings = new List<MissionRatingEntity>();
			Creator = creator;
		}
		private MissionEntity(MissionEntity from, IDictionary<Guid, Object> circularReferenceHelperDictionary) : base(from, circularReferenceHelperDictionary)
		{
			Name = from.Name;
			Description = from.Description;
			Location = from.Location;
			Aspects = from.Aspects.ToArray();
			Ratings = from.Ratings.CloneAsT(circularReferenceHelperDictionary).ToList();
			Creator = from.Creator.CloneAsT(circularReferenceHelperDictionary);
		}

		public virtual String Name { get; set; } = String.Empty;
		public virtual String Description { get; set; } = String.Empty;
		public virtual String Location { get; set; } = String.Empty;
		public virtual IEnumerable<String> Aspects { get; set; } = Array.Empty<String>();
		public virtual UserEntity? Creator { get; set; }
		public virtual ICollection<MissionRatingEntity> Ratings { get; set; }

		public override Object Clone(IDictionary<Guid, Object> circularReferenceHelperDictionary)
		{
			return new MissionEntity(this, circularReferenceHelperDictionary);
		}
	}
}
