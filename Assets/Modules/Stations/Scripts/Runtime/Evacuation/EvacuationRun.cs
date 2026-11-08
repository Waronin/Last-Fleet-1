using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Stations
{
    public enum EvacuationLeg
    {
        /// <summary>Flying to a docking port of the station.</summary>
        ToStation,

        /// <summary>Docked; people come aboard one by one.</summary>
        Boarding,

        /// <summary>Flying the people to a hangar of a ship with living space.</summary>
        ToFleet
    }

    /// <summary>One small ship ferrying people from a station to the fleet until it is told to stop.</summary>
    public sealed class EvacuationRun
    {
        public EvacuationRun(ShipAgent ship, ZoneView zone, ZoneStation station)
        {
            Ship = ship;
            Zone = zone;
            Station = station;
        }

        public ShipAgent Ship { get; }
        public ZoneView Zone { get; }
        public ZoneStation Station { get; }
        public EvacuationLeg Leg { get; set; }

        /// <summary>Time towards the next person coming aboard.</summary>
        public float BoardingProgressSeconds { get; set; }

        /// <summary>The run issues its own orders; replaced orders only end it when the player gave them.</summary>
        public bool IsIssuingOrder { get; set; }
    }
}
