using PBData.Abstractions;
using PBData.Entities;
using PBData.Extensions;

namespace UnityShuffle.Data
{
	public class MissionEntity : ExpiringEntityBase, IHasCreator<UserEntity>
	{
		public MissionEntity() { }
		public MissionEntity(String name, String description, String location, UserEntity creator, TimeSpan lifeSpan, params String[] branches) : base(lifeSpan, true, false)
		{
			Name = name;
			Description = description;
			Location = location;
			Branches = branches;
			Ratings = new List<MissionRatingEntity>();
			Creator = creator;
		}
		private MissionEntity(MissionEntity from, IDictionary<Guid, Object> circularReferenceHelperDictionary) : base(from, circularReferenceHelperDictionary)
		{
			Name = from.Name;
			Description = from.Description;
			Location = from.Location;
			Branches = from.Branches.ToArray();
			Ratings = from.Ratings.CloneAsT(circularReferenceHelperDictionary).ToList();
			Creator = from.Creator.CloneAsT(circularReferenceHelperDictionary);
		}

		public virtual String Name { get; set; } = String.Empty;
		public virtual String Description { get; set; } = String.Empty;
		public virtual String Location { get; set; } = String.Empty;
		public virtual IEnumerable<String> Branches { get; set; } = Array.Empty<String>();

		private UserEntity? creator;
		public virtual UserEntity? Creator
		{
			get => creator; set
			{
				creator = value;
				RefreshNow();
			}
		}
		public virtual ICollection<MissionRatingEntity> Ratings { get; set; }
		public override Boolean ExpiryPaused { get => Creator != null; set => _ = value; }

		public override Object Clone(IDictionary<Guid, Object> circularReferenceHelperDictionary)
		{
			return new MissionEntity(this, circularReferenceHelperDictionary);
		}
	}
}
