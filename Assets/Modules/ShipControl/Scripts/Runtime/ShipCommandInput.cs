using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFleet.ShipControl
{
    /// <summary>
    /// Mouse control of small ships in the viewed zone (GDD: Controls). Click selects the ship's
    /// squad, double click all ships of that class in the zone, drag selects the ships inside the
    /// rectangle; Shift adds. Right click orders a move in formation; Shift appends to the queue.
    /// Picking works in screen space around each ship, so tiny ships stay easy to hit.
    /// </summary>
    public sealed class ShipCommandInput : MonoBehaviour
    {
        const float DragThresholdPixels = 6f;
        const float DoubleClickSeconds = 0.35f;
        const float CapitalSpacingUnits = 25f;
        const float CapitalClearanceUnits = 15f;

        [SerializeField] Camera _camera;
        [SerializeField] ShipRegistry _registry;
        [SerializeField] ScoutingService _scouting;
        [SerializeField, Min(1f)] float _pickRadiusPixels = 18f;
        [SerializeField] Color _selectionBoxColor = new Color(0.4f, 0.8f, 1f, 0.25f);

        readonly ShipSelection _selection = new ShipSelection();
        readonly List<ShipAgent> _shipsInZone = new List<ShipAgent>();
        readonly List<ShipAgent> _picked = new List<ShipAgent>();
        readonly List<ShipAgent> _ordered = new List<ShipAgent>();
        readonly List<ShipAgent> _capitals = new List<ShipAgent>();
        readonly List<ShipAgent> _smallShips = new List<ShipAgent>();
        ZoneView _activeZone;
        bool _isDragging;
        Vector2 _dragStart;
        readonly Dictionary<object, Rect> _blockedScreenAreas = new Dictionary<object, Rect>();
        ShipAgent _lastClickedShip;
        float _lastClickTime = float.NegativeInfinity;

        public ShipSelection Selection => _selection;

        /// <summary>Short feedback for the player about what a command did.</summary>
        public event System.Action<GameText> Notice;

        /// <summary>A deposit was left-clicked. The ship selection is kept, so ships can be put to work on it.</summary>
        public event System.Action<ZoneView, ResourceDeposit> DepositClicked;

        /// <summary>Empty space was left-clicked; the selection was cleared.</summary>
        public event System.Action EmptySpaceClicked;

        void Awake()
        {
            if (_camera == null || _registry == null || _scouting == null)
            {
                Debug.LogError("ShipCommandInput needs the camera, the ship registry and the scouting service.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _registry.ShipRemoved += _selection.Remove;
            _selection.Changed += RefreshSelectionVisuals;
        }

        void OnDisable()
        {
            _registry.ShipRemoved -= _selection.Remove;
            _selection.Changed -= RefreshSelectionVisuals;
        }

        /// <summary>The zone the player is looking at; null disables mouse control, e.g. on the maps.</summary>
        public void SetActiveZone(ZoneView zone)
        {
            _activeZone = zone;
            _isDragging = false;
        }

        /// <summary>
        /// Screen area (pixels, origin bottom left) covered by a UI panel, where clicks must not reach
        /// ships. Each panel registers its own area.
        /// </summary>
        public void SetBlockedScreenArea(object owner, Rect screenArea)
        {
            _blockedScreenAreas[owner] = screenArea;
        }

        public void ClearBlockedScreenArea(object owner)
        {
            _blockedScreenAreas.Remove(owner);
        }

        bool IsOverUi(Vector2 pointer)
        {
            foreach (Rect area in _blockedScreenAreas.Values)
            {
                if (area.Contains(pointer))
                {
                    return true;
                }
            }
            return false;
        }

        public void SelectSquad(Squad squad, bool additive)
        {
            _selection.Select(squad.Members, additive);
        }

        /// <summary>
        /// Orders every selected ship to a point of a zone. Capital ships take the point itself, side
        /// by side; small ships keep their squads together in wedge slots behind them, facing the
        /// direction of travel.
        /// </summary>
        public void OrderSelected(int zoneIndex, Vector3 localPoint, bool append)
        {
            if (_selection.Count == 0)
            {
                return;
            }

            SortBySquad(_selection.Ships, _ordered);
            Vector2 facing = FormationFacing(_ordered, zoneIndex, localPoint);
            Vector3 forward = new Vector3(facing.x, 0f, facing.y).normalized;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            _capitals.Clear();
            _smallShips.Clear();
            foreach (ShipAgent ship in _ordered)
            {
                (ship.IsCapital ? _capitals : _smallShips).Add(ship);
            }

            float centerOffset = (_capitals.Count - 1) * 0.5f;
            for (int i = 0; i < _capitals.Count; i++)
            {
                Vector3 point = localPoint + right * ((i - centerOffset) * CapitalSpacingUnits);
                _capitals[i].Order(ShipOrder.MoveTo(zoneIndex, point), append);
            }

            Vector3 smallOrigin = _capitals.Count > 0 ? localPoint - forward * CapitalClearanceUnits : localPoint;
            for (int i = 0; i < _smallShips.Count; i++)
            {
                Vector3 slot = SquadFormation.ZoneOffset(i, _registry.FormationSpacingUnits, facing);
                _smallShips[i].Order(ShipOrder.MoveTo(zoneIndex, smallOrigin + slot), append);
            }
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (_activeZone == null || mouse == null || keyboard == null)
            {
                return;
            }

            bool shift = keyboard.shiftKey.isPressed;
            Vector2 pointer = mouse.position.ReadValue();
            bool overUi = IsOverUi(pointer);

            if (mouse.leftButton.wasPressedThisFrame && !overUi)
            {
                _isDragging = true;
                _dragStart = pointer;
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _isDragging)
            {
                _isDragging = false;
                if ((pointer - _dragStart).magnitude < DragThresholdPixels)
                {
                    HandleClick(pointer, shift);
                }
                else
                {
                    SelectInRectangle(ScreenRectangle(_dragStart, pointer), shift);
                }
            }

            if (!mouse.rightButton.wasPressedThisFrame || overUi || _selection.Count == 0)
            {
                return;
            }

            // Context command (GDD): right click on a deposit sends harvesters mining or scouts scanning,
            // otherwise it is a move.
            if (_activeZone.TryPickDeposit(_camera, pointer, _pickRadiusPixels, out ResourceDeposit deposit) &&
                (TryOrderMining(deposit) || TryOrderScan(deposit)))
            {
                return;
            }
            if (_activeZone.Zone.TryRaycastPlane(_camera.ScreenPointToRay(pointer), out Vector3 worldPoint))
            {
                Vector3 localPoint = _activeZone.transform.InverseTransformPoint(worldPoint);
                OrderSelected(_activeZone.Record.PointOfInterestIndex, localPoint, shift);
            }
        }

        /// <summary>Selected harvesters mine the deposit; other selected ships move next to it.</summary>
        bool TryOrderMining(ResourceDeposit deposit)
        {
            int harvesters = 0;
            int started = 0;
            _ordered.Clear();
            foreach (ShipAgent ship in _selection.Ships)
            {
                if (ship.TryGetComponent(out HarvesterMining mining))
                {
                    harvesters++;
                    if (mining.StartMining(_activeZone, deposit))
                    {
                        started++;
                    }
                }
                else
                {
                    _ordered.Add(ship);
                }
            }
            if (harvesters == 0)
            {
                return false;
            }

            foreach (ShipAgent ship in _ordered)
            {
                ship.Order(ShipOrder.MoveTo(_activeZone.Record.PointOfInterestIndex, deposit.LocalPosition), false);
            }
            Notice?.Invoke(ShipControlTexts.HarvestersMining.Format(("count", started), ("withoutSlot", harvesters - started)));
            return true;
        }

        /// <summary>A selected scout, such as a shuttle, scans a deposit that is not scanned yet.</summary>
        bool TryOrderScan(ResourceDeposit deposit)
        {
            if (deposit.IsScanned)
            {
                return false;
            }

            _ordered.Clear();
            foreach (ShipAgent ship in _selection.Ships)
            {
                if (ship.TryGetComponent(out ShipScanner scanner) && scanner.IsScout)
                {
                    _ordered.Add(ship);
                }
            }
            return _ordered.Count > 0 && _scouting.RequestScan(_activeZone, deposit, _ordered);
        }

        void HandleClick(Vector2 pointer, bool additive)
        {
            ShipAgent ship = PickShip(pointer);
            bool isDoubleClick = ship != null && ship == _lastClickedShip && Time.unscaledTime - _lastClickTime <= DoubleClickSeconds;
            _lastClickedShip = ship;
            _lastClickTime = Time.unscaledTime;

            if (ship == null)
            {
                // Inspecting a deposit keeps the selected ships, so they can be put to work on it.
                if (_activeZone.TryPickDeposit(_camera, pointer, _pickRadiusPixels, out ResourceDeposit deposit))
                {
                    DepositClicked?.Invoke(_activeZone, deposit);
                    return;
                }
                if (!additive)
                {
                    _selection.Clear();
                }
                EmptySpaceClicked?.Invoke();
                return;
            }

            _picked.Clear();
            if (isDoubleClick)
            {
                // GDD: double click selects all ships of the same type in the zone.
                foreach (ShipAgent candidate in _shipsInZone)
                {
                    if (candidate.ClassName == ship.ClassName)
                    {
                        _picked.Add(candidate);
                    }
                }
            }
            else if (_registry.Squads.TryGetSquad(ship, out Squad squad))
            {
                _picked.AddRange(squad.Members);
            }
            else
            {
                _picked.Add(ship);
            }
            _selection.Select(_picked, additive);
        }

        ShipAgent PickShip(Vector2 pointer)
        {
            _registry.CollectShipsInZone(_activeZone, _shipsInZone);
            ShipAgent nearest = null;
            float bestDistanceSquared = _pickRadiusPixels * _pickRadiusPixels;
            foreach (ShipAgent ship in _shipsInZone)
            {
                if (TryGetScreenPosition(ship, out Vector2 screen) && (screen - pointer).sqrMagnitude < bestDistanceSquared)
                {
                    bestDistanceSquared = (screen - pointer).sqrMagnitude;
                    nearest = ship;
                }
            }
            return nearest;
        }

        void SelectInRectangle(Rect screenRectangle, bool additive)
        {
            _registry.CollectShipsInZone(_activeZone, _shipsInZone);
            _picked.Clear();
            foreach (ShipAgent ship in _shipsInZone)
            {
                if (TryGetScreenPosition(ship, out Vector2 screen) && screenRectangle.Contains(screen))
                {
                    _picked.Add(ship);
                }
            }
            _selection.Select(_picked, additive);
        }

        /// <summary>Orders ships by squad and position within the squad, so leaders take the front slots.</summary>
        void SortBySquad(IReadOnlyList<ShipAgent> ships, List<ShipAgent> result)
        {
            result.Clear();
            foreach (Squad squad in _registry.Squads.Squads)
            {
                foreach (ShipAgent member in squad.Members)
                {
                    if (_selection.Contains(member))
                    {
                        result.Add(member);
                    }
                }
            }
            foreach (ShipAgent ship in ships)
            {
                if (!result.Contains(ship))
                {
                    result.Add(ship);
                }
            }
        }

        /// <summary>Formation faces from the ships towards the target, or along the jump for other zones.</summary>
        static Vector2 FormationFacing(List<ShipAgent> ships, int zoneIndex, Vector3 localPoint)
        {
            ShipAgent leader = ships[0];
            if (leader.Zone.Record.PointOfInterestIndex != zoneIndex)
            {
                return leader.JumpDirection(zoneIndex);
            }

            // World positions, because docked ships sit at their carrier's bay rather than in the zone.
            Vector3 centroid = Vector3.zero;
            int count = 0;
            foreach (ShipAgent ship in ships)
            {
                if (ship.Zone == leader.Zone)
                {
                    centroid += leader.Zone.transform.InverseTransformPoint(ship.WorldPosition);
                    count++;
                }
            }
            Vector3 toTarget = localPoint - centroid / count;
            return new Vector2(toTarget.x, toTarget.z);
        }

        bool TryGetScreenPosition(ShipAgent ship, out Vector2 screenPosition)
        {
            Vector3 point = _camera.WorldToScreenPoint(ship.transform.position);
            screenPosition = new Vector2(point.x, point.y);
            return point.z > 0f;
        }

        void RefreshSelectionVisuals()
        {
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (ship != null && ship.TryGetComponent(out ShipPresentation presentation))
                {
                    presentation.SetSelected(_selection.Contains(ship));
                }
            }
        }

        static Rect ScreenRectangle(Vector2 a, Vector2 b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        // Drag rectangle as a developer-grade overlay until the real HUD exists.
        void OnGUI()
        {
            if (!_isDragging || _activeZone == null || Mouse.current == null)
            {
                return;
            }

            Rect screen = ScreenRectangle(_dragStart, Mouse.current.position.ReadValue());
            Rect gui = new Rect(screen.xMin, Screen.height - screen.yMax, screen.width, screen.height);
            Color previous = GUI.color;
            GUI.color = _selectionBoxColor;
            GUI.DrawTexture(gui, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
