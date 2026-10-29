using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Inspection
{
    /// <summary>
    /// A station in a zone: what can be seen from outside, its type, size and docking bays. Crew,
    /// atmosphere, condition and radiation stay hidden until the station is scanned.
    /// </summary>
    public sealed class StationInspection : IInspectionTarget
    {
        readonly ZoneView _zone;
        readonly ZoneStation _station;

        public StationInspection(ZoneView zone, ZoneStation station)
        {
            _zone = zone;
            _station = station ?? throw new ArgumentNullException(nameof(station));
        }

        public ZoneStation Station => _station;
        public GameText Title => GameText.Verbatim(_station.Name);
        public bool IsValid => _zone != null;

        public void Describe(List<GameText> lines)
        {
            lines.Clear();
            lines.Add(InspectionTexts.ZoneLine.Format(("zone", _zone.Record.PointOfInterest.Name)));
            lines.Add(TypeLine(_station));
            lines.Add(InspectionTexts.StationBays.Format(("count", _station.DockingBayCount)));
            lines.Add(InspectionTexts.StationUnscanned);
        }

        public void CollectActions(List<InspectionAction> actions)
        {
            actions.Clear();
        }

        /// <summary>Type and rough size, which are visible from afar.</summary>
        public static GameText TypeLine(ZoneStation station)
        {
            return InspectionTexts.StationTypeLine.Format(("type", ZoneTexts.StationTypeNames.For(station.Type)),
                ("length", StationTypes.NominalLengthKlicks(station.Type)));
        }
    }
}
