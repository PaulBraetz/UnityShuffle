using PBData.Abstractions;
using PBData.Entities;
using PBData.Extensions;

namespace UnityShuffle.Data
{
	public class MissionRatingEntity : EntityBase, IHasCreator<UserEntity>
	{
		public MissionRatingEntity() { }

		public MissionRatingEntity(MissionRatingEntity from, IDictionary<Guid, Object> circularReferenceHelperDictionary) : base(from, circularReferenceHelperDictionary)
		{
			Review = from.Review;
			Time = from.Time;
			Creator = from.Creator!.CloneAsT(circularReferenceHelperDictionary);
		}

		public MissionRatingEntity(String review, TimeSpan time, UserEntity creator)
		{
			Review = review;
			Time = time;
			Creator = creator;
		}

		public virtual String Review { get; set; } = String.Empty;
		public virtual TimeSpan Time { get; set; }
		public virtual UserEntity? Creator { get; set; }

		public override Object Clone(IDictionary<Guid, Object> circularReferenceHelperDictionary)
		{
			return new MissionRatingEntity(this, circularReferenceHelperDictionary);
		}
	}
}
