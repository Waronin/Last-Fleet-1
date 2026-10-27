using System.Collections.Generic;
using LastFleet.Galaxy;
using LastFleet.Mining;
using LastFleet.ShipControl;
using LastFleet.Ships;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.GenerationPreview
{
    /// <summary>
    /// Developer overlay for the generation preview. Uses IMGUI and English text on purpose:
    /// player-facing UI will use the project's UI framework and Unity Localization.
    /// </summary>
    public sealed class GenerationPreviewOverlay : MonoBehaviour
    {
        const float PanelWidth = 420f;
        const float Margin = 10f;

        [SerializeField] GenerationPreviewController _controller;

        const float DoubleClickSeconds = 0.35f;

        Vector2 _scrollPosition;
        object _lastClickedEntry;
        float _lastClickTime = float.NegativeInfinity;

        void Awake()
        {
            if (_controller == null)
            {
                Debug.LogError("GenerationPreviewOverlay needs the preview controller.", this);
                enabled = false;
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Margin, Margin, PanelWidth, Screen.height - Margin * 2f));
            GUILayout.BeginVertical(GUI.skin.box);
            switch (_controller.Mode)
            {
                case GenerationPreviewMode.GalaxyMap:
                    DrawGalaxy();
                    break;
                case GenerationPreviewMode.StarSystem:
                    DrawStarSystem();
                    break;
                case GenerationPreviewMode.Zone:
                    DrawZone();
                    break;
            }
            GUILayout.Space(6f);
            GUILayout.Label(_controller.LastAction);
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                BlockPanelForShipInput(GUILayoutUtility.GetLastRect());
            }
            GUILayout.EndArea();
        }

        /// <summary>Keeps clicks on the panel from also selecting or ordering ships behind it.</summary>
        void BlockPanelForShipInput(Rect panelInArea)
        {
            // GUI coordinates start top left, Input System screen coordinates bottom left.
            Rect screen = new Rect(Margin + panelInArea.x, Screen.height - Margin - panelInArea.yMax, panelInArea.width, panelInArea.height);
            _controller.ShipInput.SetBlockedScreenArea(screen);
        }

        void DrawGalaxy()
        {
            GalaxyLayout galaxy = _controller.Galaxy;
            if (galaxy == null)
            {
                return;
            }

            StarSystemNode selected = galaxy.Systems[_controller.SelectedSystemIndex];
            GUILayout.Label($"Galaxy seed {_controller.GalaxySeed}  |  {galaxy.Systems.Count} systems  |  {_controller.VisitedSystemCount} visited");
            GUILayout.Label($"Selected: {selected.Name}{(selected.Index == galaxy.StartSystemIndex ? " (start)" : string.Empty)}");
            GUILayout.Label($"Reachable with one jump: {galaxy.GetNeighbours(selected.Index).Count}");
            GUILayout.Space(6f);
            GUILayout.Label("Click: select   Enter / double click: enter system   R: new run");
            GUILayout.Label("WASD: pan   Q/E or middle mouse: rotate   Wheel: zoom");
        }

        void DrawStarSystem()
        {
            SystemZones system = _controller.CurrentSystem;
            if (system == null)
            {
                return;
            }

            StarSystemLayout layout = system.Layout;
            GUILayout.Label($"{layout.Name}  |  {layout.Star.Class}  |  {system.RevealedCount}/{system.Zones.Count} zones revealed");
            GUILayout.Label("Click: select   C: detail scan   Enter / double click: view zone   Esc: galaxy");
            GUILayout.Label("Right click: send selected ships there (Shift: queue)   F: follow ship");
            DrawShips();
            GUILayout.Space(6f);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < system.Zones.Count; i++)
            {
                ZoneRecord zone = system.Zones[i];
                string selection = i == _controller.SelectedPointOfInterestIndex ? "> " : "   ";
                string state = zone.IsRevealed ? DescribeContents(zone.Contents) : "unexplored";
                string fuel = zone.PointOfInterest.IsFuelSource ? " [fuel]" : string.Empty;
                GUILayout.Label($"{selection}{zone.PointOfInterest.Kind}: {zone.PointOfInterest.Name}{fuel} - {state}");
            }
            GUILayout.EndScrollView();
        }

        void DrawZone()
        {
            ZoneView zone = _controller.CurrentZone;
            if (zone == null)
            {
                return;
            }

            ZoneContents contents = zone.Record.Contents;
            GUILayout.Label($"Zone {zone.Record.PointOfInterest.Name} ({zone.Record.PointOfInterest.Kind})");
            GUILayout.Label($"Radius {contents.RadiusKlicks:0} klicks  |  {contents.Stations.Count} stations  |  " +
                            $"{CountScanned(contents)}/{contents.Deposits.Count} deposits scanned");
            // Only scanned deposits contribute, so the overlay does not give away unscanned resources.
            foreach (KeyValuePair<ResourceKind, (int Count, float Remaining, float Initial)> entry in SumDeposits(contents))
            {
                (int count, float remaining, float initial) = entry.Value;
                GUILayout.Label($"  {entry.Key}: {count} scanned deposits, {remaining:0} of {initial:0} left");
            }
            ResourceDeposit nearest = _controller.NearestDeposit;
            if (nearest != null)
            {
                GUILayout.Label($"Nearest: {GenerationPreviewController.DescribeDeposit(nearest)}");
            }
            DrawShips();
            GUILayout.Space(6f);
            GUILayout.Label("Left click / drag: select ships (Shift: add)   Right click: move (Shift: queue), on a deposit: mine");
            GUILayout.Label("V: scan nearest deposit (Shift: whole zone)   X: extract nearest deposit");
            GUILayout.Label("Tab / Shift+Tab: next revealed zone   F: follow ship   Esc: system map");
        }

        static int CountScanned(ZoneContents contents)
        {
            int scanned = 0;
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                if (deposit.IsScanned)
                {
                    scanned++;
                }
            }
            return scanned;
        }

        /// <summary>Squad bar stand-in (GDD: Squad list): click selects, Shift adds, double click shows it.</summary>
        void DrawShips()
        {
            GUILayout.Space(6f);
            GUILayout.Label("Capital ships and squads (click: select, Shift: add, double click: show)");
            foreach (ShipAgent ship in _controller.Ships)
            {
                if (!ship.IsCapital || ship.Zone == null)
                {
                    continue;
                }

                string hangar = ship.TryGetComponent(out Hangar shipHangar)
                    ? $", hangar {shipHangar.DockedShips.Count}/{shipHangar.Capacity}{(shipHangar.LaunchQueueCount > 0 ? $", launching {shipHangar.LaunchQueueCount}" : string.Empty)}"
                    : string.Empty;
                string storage = ship.TryGetComponent(out RawMaterialStorage shipStorage) ? $", stored {DescribeStock(shipStorage.Stock)}" : string.Empty;
                string label = $"{(_controller.Selection.Contains(ship) ? "* " : string.Empty)}{ship.name} - {ship.Activity} " +
                               $"in {ship.Zone.Record.PointOfInterest.Name}{DescribeTarget(ship)}{hangar}{storage}";
                if (GUILayout.Button(label, GUI.skin.label))
                {
                    HandleEntryClick(ship, () => _controller.SelectShip(ship, Event.current.shift), ship);
                }
            }

            foreach (Squad squad in _controller.Squads)
            {
                if (squad.Leader == null || squad.Leader.Zone == null)
                {
                    continue;
                }

                bool selected = _controller.Selection.Contains(squad.Leader);
                string docked = squad.DockedCount > 0 ? $", {squad.DockedCount} docked" : string.Empty;
                string label = $"{(selected ? "* " : string.Empty)}{squad.Name} ({squad.Members.Count}{docked}) - {squad.Activity} " +
                               $"in {squad.Leader.Zone.Record.PointOfInterest.Name}{DescribeTarget(squad.Leader)}";
                if (GUILayout.Button(label, GUI.skin.label))
                {
                    HandleEntryClick(squad, () => _controller.SelectSquad(squad, Event.current.shift), squad.Leader);
                }
            }
            GUILayout.Label("G: form squad   H: return to hangar   Ctrl+H: recall all   L: launch selected");

            foreach (ShipAgent ship in _controller.Selection.Ships)
            {
                GUILayout.Label($"   {ship.name}: {ship.MotionPhase}, {ship.Speed:0.0} u/s ({ship.Speed * 0.1f:0.00} km/s), " +
                                $"{ship.QueuedOrderCount} queued{DescribeMining(ship)}");
            }

            GUILayout.Label($"Control groups (Ctrl+number: store, number: recall, twice: show): {DescribeGroups()}");
        }

        void HandleEntryClick(object entry, System.Action select, ShipAgent focusTarget)
        {
            bool isDoubleClick = entry == _lastClickedEntry && Time.unscaledTime - _lastClickTime <= DoubleClickSeconds;
            _lastClickedEntry = entry;
            _lastClickTime = Time.unscaledTime;

            select();
            if (isDoubleClick)
            {
                _controller.FocusShip(focusTarget);
            }
        }

        string DescribeTarget(ShipAgent ship)
        {
            if (ship.TargetZoneIndex < 0 || ship.TargetZoneIndex == ship.Zone.Record.PointOfInterestIndex)
            {
                return string.Empty;
            }

            string charge = ship.Activity == ShipActivity.ChargingJump ? $", jump in {ship.JumpChargeRemainingSeconds:0.0} s" : string.Empty;
            return $" -> {_controller.CurrentSystem.Zones[ship.TargetZoneIndex].PointOfInterest.Name}{charge}";
        }

        static string DescribeMining(ShipAgent ship)
        {
            if (!ship.TryGetComponent(out HarvesterMining mining))
            {
                return string.Empty;
            }

            string cargo = mining.Cargo.IsEmpty ? "empty" : $"{mining.Cargo.Resource} {mining.Cargo.Amount:0}/{mining.Cargo.Capacity:0}";
            string bonus = mining.State == MiningState.Extracting ? $", yield x{mining.YieldMultiplier:0.00}" : string.Empty;
            return $", mining {mining.State}, cargo {cargo}{bonus}";
        }

        static string DescribeStock(RawMaterialStock stock)
        {
            return $"Ore {stock.AmountOf(ResourceKind.Ore):0}, Ice {stock.AmountOf(ResourceKind.Ice):0}, Gas {stock.AmountOf(ResourceKind.Gas):0}";
        }

        string DescribeGroups()
        {
            List<string> parts = new List<string>();
            for (int group = 0; group < ControlGroups.GroupCount; group++)
            {
                int count = _controller.ControlGroups.Get(group).Count;
                if (count > 0)
                {
                    parts.Add($"{group}: {count}");
                }
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "none";
        }

        static string DescribeContents(ZoneContents contents)
        {
            int active = 0;
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                if (!deposit.IsDepleted)
                {
                    active++;
                }
            }
            return $"{active}/{contents.Deposits.Count} deposits, {contents.Stations.Count} stations";
        }

        static SortedDictionary<ResourceKind, (int Count, float Remaining, float Initial)> SumDeposits(ZoneContents contents)
        {
            SortedDictionary<ResourceKind, (int Count, float Remaining, float Initial)> sums =
                new SortedDictionary<ResourceKind, (int Count, float Remaining, float Initial)>();
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                if (!deposit.IsScanned)
                {
                    continue;
                }
                sums.TryGetValue(deposit.Resource, out (int Count, float Remaining, float Initial) sum);
                sums[deposit.Resource] = (sum.Count + 1, sum.Remaining + deposit.RemainingAmount, sum.Initial + deposit.InitialAmount);
            }
            return sums;
        }
    }
}
