using PBCommon;
using PBData.Abstractions;
using PBData.Entities;
using PBData.Extensions;

namespace UnityShuffle.Data
{
	public class RoomEntity : ExpiringEntityBase, ISessionAttachment, IHasName
	{
		public RoomEntity()
		{
		}

		protected RoomEntity(RoomEntity from, IDictionary<Guid, Object> circularReferenceHelperDictionary) : base(from, circularReferenceHelperDictionary)
		{
			Name = from.Name;
			Drawn = from.Drawn.CloneAsT(circularReferenceHelperDictionary);
			Deck = from.Deck.CloneAsT(circularReferenceHelperDictionary);
		}

		public RoomEntity(String name) : base(TimeSpan.MinValue, true, false)
		{
			Name = name;
		}

		public virtual String Name { get; protected set; }

		public virtual MissionEntity? Top { get => drawn.TryPeek(out MissionEntity? top) ? top : null; set => _ = value; }
		private Stack<MissionEntity> drawn = new();
		public virtual IEnumerable<MissionEntity> Drawn { get => drawn; set => drawn = new Stack<MissionEntity>(value); }

		public virtual MissionEntity? Next { get => deck.TryPeek(out MissionEntity? next) ? next : null; set => _ = value; }
		private Stack<MissionEntity> deck = new();
		public virtual IEnumerable<MissionEntity> Deck { get => deck; set => deck = new Stack<MissionEntity>(value); }

		public override Boolean ExpiryPaused { get => IsAttached; set => _ = value; }

		protected virtual Boolean IsAttached { get; set; }

		public void Add(MissionEntity mission)
		{
			deck.Push(mission);
		}

		public void Remove(MissionEntity mission)
		{
			MissionEntity? top = Top;

			removeFrom(ref deck);
			removeFrom(ref drawn);

			void removeFrom(ref Stack<MissionEntity> stack)
			{
				var newStack = new Stack<MissionEntity>();

				while (stack.TryPop(out MissionEntity? m))
				{
					if (!m.Equals(mission))
					{
						newStack.Push(m);
					}
				}

				stack = newStack;
			}
		}

		public void Shuffle()
		{
			var missions = new List<MissionEntity>();

			while (deck.TryPop(out MissionEntity? m))
			{
				missions.Add(m);
			}

			while (missions.Any())
			{
				Int32 index = Random.Shared.Next(0, missions.Count);
				MissionEntity? mission = missions.ElementAt(index);
				deck.Push(mission);
				missions.RemoveAt(index);
			}
		}

		public void Join()
		{
			while (drawn.TryPop(out MissionEntity? m))
			{
				deck.Push(m);
			}
		}

		public void Draw()
		{
			if (deck.TryPop(out MissionEntity? c))
			{
				drawn.Push(c);
			}
		}

		public void Skip()
		{
			if (Next != null)
			{
				MissionEntity? next = deck.Pop();
				var hold = new Stack<MissionEntity>();
				while (deck.TryPop(out MissionEntity? m))
				{
					hold.Push(m);
				}
				hold.Push(next);
				while (hold.TryPop(out MissionEntity? m))
				{
					deck.Push(m);
				}
			}
		}

		public override Object Clone(IDictionary<Guid, Object> circularReferenceHelperDictionary)
		{
			return new RoomEntity(this, circularReferenceHelperDictionary);
		}

		public void AttachTo(UserSessionEntity session)
		{
			IsAttached = true;
		}
		public void DetachFrom(UserSessionEntity session)
		{
			IsAttached = false;
		}
	}
}
