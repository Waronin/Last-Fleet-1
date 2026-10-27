using System;
using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.StarSystem;
using LastFleet.Zones;

namespace LastFleet.Inspection
{
    /// <summary>
    /// A point of interest on the system map: what the system scan knows about it, and the
    /// interactions view its zone or send a ship to explore it.
    /// </summary>
    public sealed class PointOfInterestInspection : IInspectionTarget
    {
        readonly SystemZones _system;
        readonly int _index;
        readonly ScoutingService _scouting;
        readonly Action<int> _viewZone;

        public PointOfInterestInspection(SystemZones system, int pointOfInterestIndex, ScoutingService scouting, Action<int> viewZone)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _index = pointOfInterestIndex;
            _scouting = scouting;
            _viewZone = viewZone;
        }

        ZoneRecord Zone => _system.Zones[_index];
        public string Title => Zone.PointOfInterest.Name;
        public bool IsValid => true;

        public void Describe(List<string> lines)
        {
            lines.Clear();
            PointOfInterest point = Zone.PointOfInterest;
            lines.Add($"{point.Kind}, {point.PositionAU.magnitude:0.00} AU from the star");
            if (point.IsFuelSource)
            {
                lines.Add("Fuel source");
            }

            if (!Zone.IsRevealed)
            {
                lines.Add("Unexplored. Send a ship to find out what is there.");
                return;
            }

            ZoneContents contents = Zone.Contents;
            int scanned = 0;
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                if (deposit.IsScanned)
                {
                    scanned++;
                }
            }
            lines.Add($"Deposits: {contents.Deposits.Count} ({scanned} scanned)");
            lines.Add($"Stations: {contents.Stations.Count}");
        }

        public void CollectActions(List<InspectionAction> actions)
        {
            actions.Clear();
            bool revealed = Zone.IsRevealed;
            actions.Add(new InspectionAction("View zone", "double click", revealed, revealed ? null : "Not explored yet.",
                () =>
                {
                    _viewZone(_index);
                    return null;
                }));
            actions.Add(new InspectionAction("Explore", null, !revealed, revealed ? "Already explored." : null,
                () => _scouting.RequestExploration(_index, Zone.PointOfInterest) ? null : "No ship available to explore."));
        }
    }
}
