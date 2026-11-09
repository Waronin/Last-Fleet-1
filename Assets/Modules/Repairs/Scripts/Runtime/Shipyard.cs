using LastFleet.Ships;

namespace LastFleet.Repairs
{
    /// <summary>
    /// The engineers of one capital ship. They work on one ship at a time: the capital ship itself
    /// or a small ship in its hangar, in the order the player set.
    /// </summary>
    public sealed class Shipyard
    {
        internal Shipyard(ShipAgent owner)
        {
            Owner = owner;
        }

        public ShipAgent Owner { get; }

        /// <summary>The ship being repaired, or null.</summary>
        public ShipAgent Current => Queue.Current;

        /// <summary>Ships in the queue behind the current one.</summary>
        public int WaitingCount => Queue.Count > 0 ? Queue.Count - 1 : 0;

        /// <summary>True while the current repair stands still for lack of metal.</summary>
        public bool IsShortOfMetal { get; internal set; }

        internal RepairQueue<ShipAgent> Queue { get; } = new RepairQueue<ShipAgent>();

        /// <summary>Whether these engineers can reach the ship: it is the owner or sits in the owner's hangar.</summary>
        public bool CanReach(ShipAgent ship)
        {
            if (ship == null)
            {
                return false;
            }
            return ship == Owner || (ship.DockedIn != null && ship.DockedIn.Carrier == Owner);
        }
    }
}
