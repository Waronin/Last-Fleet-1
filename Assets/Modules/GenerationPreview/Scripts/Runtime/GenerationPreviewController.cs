using System.Collections.Generic;
using LastFleet.CameraControl;
using LastFleet.Exploration;
using LastFleet.Galaxy;
using LastFleet.Inspection;
using LastFleet.Localization;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.ShipControl;
using LastFleet.Ships;
using LastFleet.Space;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFleet.GenerationPreview
{
    public enum GenerationPreviewMode
    {
        GalaxyMap,
        StarSystem,
        Zone
    }

    /// <summary>
    /// Developer scene for generation and ship control. A run starts in the jump point zone of the
    /// start system with one harvester. Zone state is kept per system for the whole run.
    /// Galaxy map: click selects, Enter / double click enters a system, R starts a new run.
    /// System map: click selects a point of interest, C detail-scans it, Enter / double click views its
    /// zone, right click sends the selected ships there (Shift appends), Esc returns to the galaxy.
    /// Zone: ship control via ShipCommandInput, Tab / Shift+Tab cycles revealed zones, F follows the
    /// selected ship, X extracts half of the nearest deposit, Esc opens the system map.
    /// </summary>
    public sealed class GenerationPreviewController : MonoBehaviour
    {
        const float DoubleClickSeconds = 0.35f;
        const float MapViewDistanceFactor = 0.9f;
        const float SystemViewDistanceFactor = 1.6f;
        const float ZoneViewDistanceUnits = 120f;
        const float DeveloperExtractFraction = 0.5f;
        const int HarvestersPerSquad = 3;
        const float DeveloperLightDamage = 25f;
        const float DeveloperHeavyDamage = 100f;
        static readonly GameText DeveloperDamageCause = GameText.Verbatim("developer damage");
        static readonly Vector3 RefineryArrivalPosition = new Vector3(0f, 0f, 0f);

        [SerializeField] int _galaxySeed = 1;
        [SerializeField] GalaxySettings _galaxySettings = new GalaxySettings();
        [SerializeField] GalaxyMapView _galaxyMapView;
        [SerializeField] StarSystemView _starSystemView;
        [SerializeField] ZoneWorld _zoneWorld;
        [SerializeField] SpaceEnvironment _environment;
        [SerializeField] RtsCameraRig _cameraRig;
        [SerializeField] ShipRegistry _shipRegistry;
        [SerializeField] ShipCommandInput _shipInput;
        [SerializeField] ControlGroupInput _groupInput;
        [SerializeField] HangarCommandInput _hangarInput;
        [SerializeField] MiningCoordinator _mining;
        [SerializeField] ScoutingService _scouting;
        [SerializeField] InspectionPanel _inspection;
        [SerializeField] InspectionPanel _shipPanel;
        [Tooltip("Spawned on arrival in a system until fleet jumps carry the fleet along.")]
        [SerializeField] ShipAgent _refineryPrefab;
        [SerializeField] ShipAgent _harvesterPrefab;
        [SerializeField, Min(1f)] float _pickRadiusPixels = 14f;

        readonly List<ShipAgent> _singleShip = new List<ShipAgent>(1);
        ShipSelectionInspection _shipInspection;
        RunZoneRegistry _runZones;
        float _lastClickTime = float.NegativeInfinity;
        int _lastClickedIndex = -1;

        public GenerationPreviewMode Mode { get; private set; }
        public int GalaxySeed => _galaxySeed;
        public GalaxyLayout Galaxy => _galaxyMapView.Galaxy;
        public int SelectedSystemIndex => _galaxyMapView.SelectedSystemIndex;
        public SystemZones CurrentSystem { get; private set; }
        public int SelectedPointOfInterestIndex => _starSystemView.SelectedPointOfInterestIndex;
        public ZoneView CurrentZone { get; private set; }
        public ShipSelection Selection => _shipInput.Selection;
        public IReadOnlyList<ShipAgent> Ships => _shipRegistry.Ships;
        public IReadOnlyList<Squad> Squads => _shipRegistry.Squads.Squads;
        public ControlGroups ControlGroups => _groupInput.Groups;
        public ShipCommandInput ShipInput => _shipInput;
        public int VisitedSystemCount => _runZones != null ? _runZones.VisitedSystemCount : 0;
        public GameText LastAction { get; private set; } = GameText.Verbatim(string.Empty);

        void Awake()
        {
            if (_galaxyMapView == null || _starSystemView == null || _zoneWorld == null || _environment == null ||
                _cameraRig == null || _shipRegistry == null || _shipInput == null || _groupInput == null ||
                _hangarInput == null || _mining == null || _scouting == null || _inspection == null || _shipPanel == null ||
                _refineryPrefab == null || _harvesterPrefab == null)
            {
                Debug.LogError("GenerationPreviewController is missing a scene reference.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _zoneWorld.ZoneRevealed += OnZoneRevealed;
            _groupInput.FocusRequested += FocusShip;
            _groupInput.Notice += ShowNotice;
            _hangarInput.Notice += ShowNotice;
            _shipInput.Notice += ShowNotice;
            _mining.Notice += ShowNotice;
            _scouting.Notice += ShowNotice;
            _inspection.Notice += ShowNotice;
            _shipInput.DepositClicked += InspectDeposit;
            _shipInput.EmptySpaceClicked += _inspection.Clear;
            _shipRegistry.ShipLost += OnShipLost;
            _shipRegistry.JumpRefusedForFuel += OnJumpRefusedForFuel;
            Selection.Changed += UpdateShipPanel;
        }

        /// <summary>The ship window follows the selection: open while ships are selected.</summary>
        void UpdateShipPanel()
        {
            if (Selection.Count == 0)
            {
                _shipPanel.Clear();
                return;
            }
            _shipInspection ??= new ShipSelectionInspection(Selection, _shipRegistry, _hangarInput);
            _shipPanel.Show(_shipInspection);
        }

        void OnDisable()
        {
            _zoneWorld.ZoneRevealed -= OnZoneRevealed;
            _groupInput.FocusRequested -= FocusShip;
            _groupInput.Notice -= ShowNotice;
            _hangarInput.Notice -= ShowNotice;
            _shipInput.Notice -= ShowNotice;
            _mining.Notice -= ShowNotice;
            _scouting.Notice -= ShowNotice;
            _inspection.Notice -= ShowNotice;
            _shipInput.DepositClicked -= InspectDeposit;
            _shipInput.EmptySpaceClicked -= _inspection.Clear;
            _shipRegistry.ShipLost -= OnShipLost;
            _shipRegistry.JumpRefusedForFuel -= OnJumpRefusedForFuel;
            Selection.Changed -= UpdateShipPanel;
        }

        void InspectDeposit(ZoneView zone, ResourceDeposit deposit)
        {
            _inspection.Show(new DepositInspection(zone, deposit, _mining, _scouting, () => Selection.Ships));
        }

        void InspectPointOfInterest(int index)
        {
            _inspection.Show(new PointOfInterestInspection(CurrentSystem, index, _scouting, ViewZone));
        }

        public void SelectSquad(Squad squad, bool additive)
        {
            _shipInput.SelectSquad(squad, additive);
        }

        public void SelectShip(ShipAgent ship, bool additive)
        {
            _singleShip.Clear();
            _singleShip.Add(ship);
            Selection.Select(_singleShip, additive);
        }

        /// <summary>Shows the zone of a ship with the camera on it.</summary>
        public void FocusShip(ShipAgent ship)
        {
            if (ship == null || ship.Zone == null)
            {
                return;
            }

            _starSystemView.SelectPointOfInterest(ship.Zone.Record.PointOfInterestIndex);
            ShowZone(ship.Zone);
            _cameraRig.SnapTo(ship.WorldPosition, ZoneViewDistanceUnits);
        }

        void ShowNotice(GameText message)
        {
            LastAction = message;
        }

        void Start()
        {
            StartNewRun();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null)
            {
                return;
            }
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                LastAction = GameText.Verbatim($"Language: {LanguageSelection.SelectNext()}");
            }

            switch (Mode)
            {
                case GenerationPreviewMode.GalaxyMap:
                    HandleGalaxyInput(keyboard, mouse);
                    break;
                case GenerationPreviewMode.StarSystem:
                    HandleStarSystemInput(keyboard, mouse);
                    break;
                case GenerationPreviewMode.Zone:
                    HandleZoneInput(keyboard);
                    break;
            }
        }

        void HandleGalaxyInput(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard.rKey.wasPressedThisFrame)
            {
                _galaxySeed++;
                StartNewRun();
            }
            else if (WasEnterPressed(keyboard))
            {
                EnterStarSystem(SelectedSystemIndex);
            }
            else if (mouse.leftButton.wasPressedThisFrame &&
                     _galaxyMapView.TryPickSystem(_cameraRig.Camera, mouse.position.ReadValue(), _pickRadiusPixels, out int systemIndex))
            {
                if (RegisterClick(systemIndex))
                {
                    EnterStarSystem(systemIndex);
                }
                else
                {
                    _galaxyMapView.Select(systemIndex);
                }
            }
        }

        void HandleStarSystemInput(Keyboard keyboard, Mouse mouse)
        {
            Vector2 pointer = mouse.position.ReadValue();
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                LeaveStarSystem();
            }
            else if (keyboard.cKey.wasPressedThisFrame)
            {
                DetailScan(SelectedPointOfInterestIndex);
            }
            else if (keyboard.fKey.wasPressedThisFrame)
            {
                FollowSelectedShip();
            }
            else if (WasEnterPressed(keyboard))
            {
                ViewZone(SelectedPointOfInterestIndex);
            }
            else if (mouse.rightButton.wasPressedThisFrame && Selection.Count > 0 &&
                     _starSystemView.TryPickPointOfInterest(_cameraRig.Camera, pointer, _pickRadiusPixels, out int targetIndex))
            {
                // Arrive at the zone center, which every zone keeps free for arrivals.
                _shipInput.OrderSelected(targetIndex, Vector3.zero, keyboard.shiftKey.isPressed);
                LastAction = GenerationPreviewTexts.OrderedToZone.Format(("count", Selection.Count), ("zone", CurrentSystem.Zones[targetIndex].PointOfInterest.Name));
            }
            else if (mouse.leftButton.wasPressedThisFrame &&
                     _starSystemView.TryPickPointOfInterest(_cameraRig.Camera, pointer, _pickRadiusPixels, out int index))
            {
                if (RegisterClick(index))
                {
                    ViewZone(index);
                }
                else
                {
                    _starSystemView.SelectPointOfInterest(index);
                    InspectPointOfInterest(index);
                }
            }
        }

        void HandleZoneInput(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ShowStarSystemMap();
            }
            else if (keyboard.tabKey.wasPressedThisFrame)
            {
                CycleRevealedZone(keyboard.shiftKey.isPressed ? -1 : 1);
            }
            else if (keyboard.fKey.wasPressedThisFrame)
            {
                FollowSelectedShip();
            }
            else if (keyboard.xKey.wasPressedThisFrame)
            {
                ExtractNearestDeposit();
            }
            else if (keyboard.vKey.wasPressedThisFrame)
            {
                ScanDeposits(keyboard.shiftKey.isPressed);
            }
            else if (keyboard.kKey.wasPressedThisFrame)
            {
                DamageSelectedShips(keyboard.shiftKey.isPressed ? DeveloperHeavyDamage : DeveloperLightDamage);
            }
            else if (keyboard.jKey.wasPressedThisFrame)
            {
                EmptySelectedTanks();
            }
        }

        // Developer shortcut to try the out-of-fuel rules without flying a tank dry.
        void EmptySelectedTanks()
        {
            foreach (ShipAgent ship in Selection.Ships)
            {
                ship.Fuel.Tank.Drain(ship.Fuel.Tank.Amount);
            }
            LastAction = GameText.Verbatim($"Emptied the tanks of {Selection.Count} ship(s).");
        }

        // Developer stand-in for enemies until combat exists.
        void DamageSelectedShips(float damage)
        {
            _singleShip.Clear();
            _singleShip.AddRange(Selection.Ships);
            foreach (ShipAgent ship in _singleShip)
            {
                if (ship.TryGetComponent(out ShipDurability durability))
                {
                    durability.ApplyDamage(damage, DeveloperDamageCause);
                }
            }
            LastAction = GameText.Verbatim($"Dealt {damage:0} damage to {_singleShip.Count} ship(s).");
        }

        void OnShipLost(ShipAgent ship, GameText cause)
        {
            LastAction = ShipTexts.ShipDestroyed.Format(("ship", ship.name), ("cause", cause));
        }

        void OnJumpRefusedForFuel(ShipAgent ship, int targetZoneIndex, float requiredFuel)
        {
            string target = _zoneWorld.CurrentSystem.Zones[targetZoneIndex].PointOfInterest.Name;
            LastAction = ShipTexts.JumpRefusedForFuel.Format(("ship", ship.name), ("zone", target), ("needed", requiredFuel), ("left", ship.Fuel.Tank.Amount));
        }

        /// <summary>Deposit nearest to the camera pivot in the viewed zone, or null.</summary>
        public ResourceDeposit NearestDeposit
        {
            get
            {
                if (CurrentZone == null)
                {
                    return null;
                }
                int index = CurrentZone.FindNearestDeposit(_cameraRig.Pivot);
                return index >= 0 ? CurrentZone.Record.Contents.Deposits[index] : null;
            }
        }

        // Stand-in for ship scanners: scans the deposit nearest to the camera, or every deposit in the zone.
        void ScanDeposits(bool wholeZone)
        {
            if (wholeZone)
            {
                foreach (ResourceDeposit deposit in CurrentZone.Record.Contents.Deposits)
                {
                    deposit.Scan();
                }
                LastAction = GameText.Verbatim($"Scanned all {CurrentZone.Record.Contents.Deposits.Count} deposits in this zone.");
            }
            else if (NearestDeposit != null)
            {
                ResourceDeposit deposit = NearestDeposit;
                deposit.Scan();
                LastAction = GameText.Verbatim($"Scanned: {DescribeDeposit(deposit)}.");
            }
            CurrentZone.Refresh();
        }

        public static string DescribeDeposit(ResourceDeposit deposit)
        {
            if (!deposit.IsScanned)
            {
                return $"{deposit.SizeClass} {deposit.Shape}, not scanned";
            }

            string hazard = deposit.Hazard == DepositHazard.None ? "no hazard" : deposit.Hazard.ToString();
            return $"{deposit.SizeClass} {deposit.Shape}: {deposit.Resource} {deposit.RemainingAmount:0} of {deposit.InitialAmount:0}, " +
                   $"{deposit.MiningSlotCount} mining slot(s), {hazard}";
        }

        void StartNewRun()
        {
            _runZones = new RunZoneRegistry();
            _zoneWorld.Clear();
            _starSystemView.Clear();
            GalaxyLayout galaxy = GalaxyGenerator.Generate((ulong)_galaxySeed, _galaxySettings);
            _galaxyMapView.Show(galaxy);
            EnterStarSystem(galaxy.StartSystemIndex);
            LastAction = GameText.Verbatim($"New run with galaxy seed {_galaxySeed}; arrived at the jump point.");
        }

        void ShowGalaxyMap()
        {
            _inspection.Clear();
            Mode = GenerationPreviewMode.GalaxyMap;
            _shipInput.SetActiveZone(null);
            _starSystemView.SetVisible(false);
            _galaxyMapView.SetVisible(true);
            _environment.SetSun(null);

            _cameraRig.SetBounds(_galaxyMapView.transform.position, _galaxyMapView.ExtentUnits);
            _cameraRig.SnapTo(_galaxyMapView.GetSystemWorldPosition(SelectedSystemIndex), _galaxyMapView.ExtentUnits * MapViewDistanceFactor);
        }

        void EnterStarSystem(int systemIndex)
        {
            _galaxyMapView.Select(systemIndex);
            if (!_runZones.TryGetSystem(systemIndex, out SystemZones system))
            {
                StarSystemNode node = Galaxy.Systems[systemIndex];
                system = _runZones.RegisterSystem(systemIndex, StarSystemGenerator.Generate(node.SystemSeed, node.Name));
            }

            CurrentSystem = system;
            _starSystemView.SetVisible(true);
            _starSystemView.Show(system.Layout);
            for (int i = 0; i < system.Zones.Count; i++)
            {
                if (system.Zones[i].IsRevealed)
                {
                    _starSystemView.SetMarkerStyle(i, PointOfInterestMarkerStyle.Explored);
                }
            }

            // Revealed zones of an earlier visit are rebuilt from the stored run state; the fleet
            // always arrives at the jump point, which is revealed on arrival.
            _zoneWorld.Show(system);
            int arrivalIndex = system.Layout.ArrivalPointOfInterestIndex;
            ZoneView arrival = _zoneWorld.Reveal(arrivalIndex);
            _starSystemView.SetMarkerStyle(arrivalIndex, PointOfInterestMarkerStyle.Explored);
            _starSystemView.SelectPointOfInterest(arrivalIndex);

            // Stand-in until fleet jumps exist: every arrival brings a refinery with two harvester squads in its hangar.
            ShipAgent refinery = _shipRegistry.Spawn(_refineryPrefab, arrival, RefineryArrivalPosition, 0f);
            Hangar hangar = refinery.GetComponent<Hangar>();
            Squad firstSquad = _shipRegistry.SpawnSquadDocked(_harvesterPrefab, hangar, HarvestersPerSquad);
            _shipRegistry.SpawnSquadDocked(_harvesterPrefab, hangar, HarvestersPerSquad);
            _shipInput.SelectSquad(firstSquad, false);

            ShowZone(arrival);
            LastAction = GenerationPreviewTexts.ArrivedInSystem.Format(("system", system.Layout.Name), ("known", system.RevealedCount));
        }

        void LeaveStarSystem()
        {
            _zoneWorld.Clear();
            _starSystemView.Clear();
            CurrentSystem = null;
            CurrentZone = null;
            LastAction = GameText.Verbatim("Left the system; zone state is kept, its ships are lost (no hangars yet).");
            ShowGalaxyMap();
        }

        void ShowStarSystemMap()
        {
            _inspection.Clear();
            Mode = GenerationPreviewMode.StarSystem;
            CurrentZone = null;
            _shipInput.SetActiveZone(null);
            _galaxyMapView.SetVisible(false);
            _starSystemView.SetVisible(true);
            _zoneWorld.SetSunEnabled(false);
            _environment.SetSun(null);

            Vector3 center = _starSystemView.transform.position;
            _cameraRig.SetBounds(center, _starSystemView.ExtentUnits);
            _cameraRig.SnapTo(center, _starSystemView.ExtentUnits * SystemViewDistanceFactor);
        }

        void DetailScan(int pointOfInterestIndex)
        {
            if (CurrentSystem.Zones[pointOfInterestIndex].IsRevealed)
            {
                LastAction = GenerationPreviewTexts.ZoneAlreadyRevealed.Format(("zone", CurrentSystem.Zones[pointOfInterestIndex].PointOfInterest.Name));
                return;
            }
            _zoneWorld.Reveal(pointOfInterestIndex);
        }

        void ViewZone(int pointOfInterestIndex)
        {
            if (!_zoneWorld.TryGetView(pointOfInterestIndex, out ZoneView view))
            {
                LastAction = GenerationPreviewTexts.ZoneUnexplored;
                return;
            }
            _starSystemView.SelectPointOfInterest(pointOfInterestIndex);
            ShowZone(view);
        }

        void ShowZone(ZoneView view)
        {
            _inspection.Clear();
            Mode = GenerationPreviewMode.Zone;
            CurrentZone = view;
            _starSystemView.SetVisible(false);
            _zoneWorld.LightZone(view);
            _environment.SetSun(_zoneWorld.ZoneSun);
            _shipInput.SetActiveZone(view);

            _cameraRig.SetBounds(view.Zone.Center, view.Zone.RadiusUnits);
            _cameraRig.SnapTo(view.Zone.Center, ZoneViewDistanceUnits);
        }

        void FollowSelectedShip()
        {
            if (Selection.Count == 0)
            {
                LastAction = GenerationPreviewTexts.NoShipSelected;
                return;
            }

            FocusShip(Selection.Ships[0]);
        }

        void OnZoneRevealed(ZoneView view)
        {
            if (_starSystemView.Layout == null)
            {
                return;
            }
            _starSystemView.SetMarkerStyle(view.Record.PointOfInterestIndex, PointOfInterestMarkerStyle.Explored);
            LastAction = GenerationPreviewTexts.ZoneRevealed.Format(("zone", view.Record.PointOfInterest.Name));
        }

        void CycleRevealedZone(int direction)
        {
            List<int> revealed = new List<int>();
            foreach (ZoneRecord zone in CurrentSystem.Zones)
            {
                if (zone.IsRevealed)
                {
                    revealed.Add(zone.PointOfInterestIndex);
                }
            }

            int current = revealed.IndexOf(CurrentZone.Record.PointOfInterestIndex);
            int next = (current + direction + revealed.Count) % revealed.Count;
            ViewZone(revealed[next]);
        }

        // Stand-in for mining until harvesters can mine; demonstrates that depletion is kept for the run.
        void ExtractNearestDeposit()
        {
            int index = CurrentZone.FindNearestDeposit(_cameraRig.Pivot);
            if (index < 0)
            {
                LastAction = GameText.Verbatim("No deposits left in this zone.");
                return;
            }

            ResourceDeposit deposit = CurrentZone.Record.Contents.Deposits[index];
            float extracted = deposit.Extract(deposit.InitialAmount * DeveloperExtractFraction);
            CurrentZone.Refresh();
            LastAction = GameText.Verbatim($"Extracted {extracted:0} {deposit.Resource}; {deposit.RemainingAmount:0} of {deposit.InitialAmount:0} left.");
        }

        bool RegisterClick(int index)
        {
            bool isDoubleClick = index == _lastClickedIndex && Time.unscaledTime - _lastClickTime <= DoubleClickSeconds;
            _lastClickTime = Time.unscaledTime;
            _lastClickedIndex = index;
            return isDoubleClick;
        }

        static bool WasEnterPressed(Keyboard keyboard)
        {
            return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
        }
    }
}
