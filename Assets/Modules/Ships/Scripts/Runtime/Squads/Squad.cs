using System.Collections.Generic;
using LastFleet.Shared.Text;

namespace LastFleet.Ships
{
    /// <summary>
    /// A group of small ships of the same class that flies in formation and takes orders as a whole
    /// (GDD: Squads). The first member leads the formation. Membership is managed by <see cref="SquadRoster"/>.
    /// </summary>
    public sealed class Squad
    {
        readonly List<ShipAgent> _members = new List<ShipAgent>();

        internal Squad(int number, string className)
        {
            Number = number;
            ClassName = className;
        }

        public int Number { get; }
        public string ClassName { get; }
        /// <summary>Technical name for logs and developer tools; players see <see cref="DisplayName"/>.</summary>
        public string Name => $"{ClassName} Squad {Number}";

        public GameText DisplayName => ShipTexts.SquadName.Format(("class", ShipTexts.Classes.For(ClassName)), ("number", Number));
        public IReadOnlyList<ShipAgent> Members => _members;
        public ShipAgent Leader => _members.Count > 0 ? _members[0] : null;

        /// <summary>The most urgent activity of any member, for status displays.</summary>
        public ShipActivity Activity
        {
            get
            {
                // Docked is the least urgent, so a squad only reads as docked when every member is.
                ShipActivity activity = ShipActivity.Docked;
                foreach (ShipAgent member in _members)
                {
                    if (Urgency(member.Activity) > Urgency(activity))
                    {
                        activity = member.Activity;
                    }
                }
                return activity;
            }
        }

        public int DockedCount
        {
            get
            {
                int count = 0;
                foreach (ShipAgent member in _members)
                {
                    if (member.IsDocked)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>GDD: a squad has a home hangar; it is the leader's.</summary>
        public Hangar HomeHangar => Leader != null ? Leader.HomeHangar : null;

        static int Urgency(ShipActivity activity)
        {
            return activity switch
            {
                ShipActivity.ChargingJump => 4,
                ShipActivity.Docking => 3,
                ShipActivity.Moving => 2,
                ShipActivity.Idle => 1,
                _ => 0
            };
        }

        internal void Add(ShipAgent ship)
        {
            _members.Add(ship);
        }

        internal bool Remove(ShipAgent ship)
        {
            return _members.Remove(ship);
        }
    }
}
