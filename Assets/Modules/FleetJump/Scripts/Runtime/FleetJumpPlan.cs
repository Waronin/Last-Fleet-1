using System.Collections.Generic;
using LastFleet.Ships;

namespace LastFleet.FleetJump
{
    /// <summary>A capital ship and what its jump costs it.</summary>
    public readonly struct FleetJumpPassage
    {
        public FleetJumpPassage(ShipAgent ship, float fuelCost, int fullRangeJumpsLeftAfter)
        {
            Ship = ship;
            FuelCost = fuelCost;
            FullRangeJumpsLeftAfter = fullRangeJumpsLeftAfter;
        }

        public ShipAgent Ship { get; }
        public float FuelCost { get; }

        /// <summary>How many full-range jumps the tank still allows after this one (GDD: the map shows the jumps left).</summary>
        public int FullRangeJumpsLeftAfter { get; }
        public float FuelAfterJump => Ship.Fuel.Tank.Amount - FuelCost;
    }

    /// <summary>
    /// What a jump to a system would do right now (GDD: the player is warned before the jump and must
    /// confirm it): which capital ships go and what they pay, which stay behind for lack of fuel and
    /// which small ships are not in a hangar of a jumping ship.
    /// </summary>
    public sealed class FleetJumpPlan
    {
        readonly List<FleetJumpPassage> _jumping = new List<FleetJumpPassage>();
        readonly List<FleetJumpPassage> _outOfFuel = new List<FleetJumpPassage>();
        readonly List<ShipAgent> _smallShipsLeftBehind = new List<ShipAgent>();

        public FleetJumpPlan(int targetSystemIndex, string targetName, float distanceLightYears)
        {
            TargetSystemIndex = targetSystemIndex;
            TargetName = targetName;
            DistanceLightYears = distanceLightYears;
        }

        public int TargetSystemIndex { get; }
        public string TargetName { get; }
        public float DistanceLightYears { get; }
        public IReadOnlyList<FleetJumpPassage> Jumping => _jumping;
        public IReadOnlyList<FleetJumpPassage> OutOfFuel => _outOfFuel;
        public IReadOnlyList<ShipAgent> SmallShipsLeftBehind => _smallShipsLeftBehind;
        public bool CanJump => _jumping.Count > 0;

        internal void AddJumping(ShipAgent ship, float fuelCost, int fullRangeJumpsLeftAfter)
        {
            _jumping.Add(new FleetJumpPassage(ship, fuelCost, fullRangeJumpsLeftAfter));
        }

        internal void AddOutOfFuel(ShipAgent ship, float fuelCost)
        {
            _outOfFuel.Add(new FleetJumpPassage(ship, fuelCost, 0));
        }

        internal void AddSmallShipLeftBehind(ShipAgent ship)
        {
            _smallShipsLeftBehind.Add(ship);
        }
    }
}
