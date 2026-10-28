using System;
using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.Shared.Text;
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
        public GameText Title => GameText.Verbatim(Zone.PointOfInterest.Name);
        public bool IsValid => true;

        public void Describe(List<GameText> lines)
        {
            lines.Clear();
            PointOfInterest point = Zone.PointOfInterest;
            lines.Add(InspectionTexts.PointOfInterestKindLine.Format(("kind", StarSystemTexts.PointOfInterestKinds.For(point.Kind)),
                ("distance", point.PositionAU.magnitude)));
            if (point.IsFuelSource)
            {
                lines.Add(InspectionTexts.FuelSource);
            }

            if (!Zone.IsRevealed)
            {
                lines.Add(InspectionTexts.Unexplored);
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
            lines.Add(InspectionTexts.DepositCount.Format(("count", contents.Deposits.Count), ("scanned", scanned)));
            lines.Add(InspectionTexts.StationCount.Format(("count", contents.Stations.Count)));
        }

        public void CollectActions(List<InspectionAction> actions)
        {
            actions.Clear();
            bool revealed = Zone.IsRevealed;
            actions.Add(new InspectionAction(InspectionTexts.ViewZoneAction, InspectionTexts.DoubleClickHint, revealed,
                revealed ? null : InspectionTexts.NotExploredYet,
                () =>
                {
                    _viewZone(_index);
                    return null;
                }));
            actions.Add(new InspectionAction(InspectionTexts.ExploreAction, null, !revealed, revealed ? InspectionTexts.AlreadyExplored : null,
                () => _scouting.RequestExploration(_index, Zone.PointOfInterest) ? null : ExplorationTexts.NoShipToExplore));
        }
    }
}
