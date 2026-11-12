using System.Collections.Generic;
using LastFleet.CameraControl;
using LastFleet.Combat;
using LastFleet.Exploration;
using LastFleet.FleetJump;
using LastFleet.Galaxy;
using LastFleet.Hud;
using LastFleet.Inspection;
using LastFleet.Localization;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Provisions;
using LastFleet.Pursuit;
using LastFleet.Quests;
using LastFleet.Quests.Meltdown;
using LastFleet.Repairs;
using LastFleet.TacticalWeapons;
using LastFleet.Rewards;
using LastFleet.RunEnd;
using LastFleet.Shared.Text;
using LastFleet.ShipControl;
using LastFleet.Ships;
using LastFleet.Stations;
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
    /// start system with the starting fleet; later systems are reached only by fleet jumps, which
    /// carry the fleet along. Zone state is kept per system for the whole run.
    /// Galaxy map: click selects, Enter / double click on a system in range opens the jump warning
    /// (on the current system: back to the system map), a click only marks a target while range and
    /// lines stay on the fleet's system, Esc returns to the zone viewed last, R starts a new run. The
    /// "jump ready" button of the jump status opens the galaxy map.
    /// System map: click selects a point of interest, C detail-scans it, Enter / double click views its
    /// zone, right click sends the selected ships there (Shift appends), Esc opens the galaxy map.
    /// Zone: ship control via ShipCommandInput, Tab / Shift+Tab cycles revealed zones, F follows the
    /// selected ship, X extracts half of the nearest deposit, Esc opens the system map.
    /// Every arrival in a system starts the pursuit countdown and the jump drive's cooldown; T skips
    /// both ahead by a minute.
    /// </summary>
    public sealed class GenerationPreviewController : MonoBehaviour
    {
        const float DoubleClickSeconds = 0.35f;
        const float MapViewDistanceFactor = 0.9f;
        const float SystemViewDistanceFactor = 1.6f;
        const float ZoneViewDistanceUnits = 120f;
        const float DeveloperExtractFraction = 0.5f;
        const int HarvestersPerSquad = 3;
        const int ShuttlesPerSquad = 2;
        const int FightersPerSquad = 4;
        const int MaximumRecentNotices = 4;
        const float DeveloperLightDamage = 25f;
        const float DeveloperHeavyDamage = 100f;
        const float DeveloperPursuitSkipSeconds = 60f;
        // Capital ships are up to about 16 units long; 30 units keep them clear of each other.
        const int ArrivalShipsPerRow = 3;
        const float ArrivalSpacingUnits = 30f;
        static readonly GameText DeveloperDamageCause = GameText.Verbatim("developer damage");
        static readonly Vector3 RefineryArrivalPosition = new Vector3(0f, 0f, 0f);
        static readonly Vector3 LinerArrivalPosition = new Vector3(-30f, 0f, 12f);
        static readonly Vector3 CruiserArrivalPosition = new Vector3(30f, 0f, 12f);
        static readonly Vector3 TankerArrivalPosition = new Vector3(0f, 0f, -22f);

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
        [SerializeField] FleetPopulation _population;
        [SerializeField] DockingService _docking;
        [SerializeField] SearchService _search;
        [SerializeField] RewardDelivery _rewards;
        [SerializeField] CombatService _combat;
        [SerializeField] PursuitClock _pursuit;
        [SerializeField] ProninAttack _proninAttack;
        [SerializeField] FleetJumpService _fleetJump;
        [SerializeField] FleetJumpDialog _jumpDialog;
        [SerializeField] JumpStatusIndicator _jumpStatus;
        [SerializeField] InspectionPanel _inspection;
        [SerializeField] InspectionPanel _shipPanel;
        [SerializeField] QuestDirector _quests;
        [Tooltip("Optional. While the end screen is open, the preview takes no input.")]
        [SerializeField] RunEndWindow _runEndWindow;
        [Tooltip("Optional. Its warnings go into the notice feed.")]
        [SerializeField] FleetWaterSupply _water;
        [Tooltip("Optional. Lets stations be evacuated from the station panel.")]
        [SerializeField] EvacuationService _evacuation;
        [Tooltip("Optional. Its notices about station requests go into the notice feed.")]
        [SerializeField] StationRequestBoard _requests;
        [Tooltip("Optional. Lets the player order hull repairs from the ship window.")]
        [SerializeField] RepairService _repairs;
        [Tooltip("Optional. EMP missiles of the warships: firing, building and their notices.")]
        [SerializeField] EmpService _emp;
        [SerializeField] EmpTargetingInput _empTargeting;
        [Tooltip("Optional. Small ships fly home to refuel below their threshold; set in the ship window.")]
        [SerializeField] FuelReturnService _fuelReturn;
        [Tooltip("Spawned on arrival in a system until fleet jumps carry the fleet along.")]
        [SerializeField] ShipAgent _refineryPrefab;
        [SerializeField] ShipAgent _tankerPrefab;
        [SerializeField] ShipAgent _linerPrefab;
        [SerializeField] ShipAgent _harvesterPrefab;
        [SerializeField] ShipAgent _shuttlePrefab;
        [SerializeField] ShipAgent _cruiserPrefab;
        [SerializeField] ShipAgent _fighterPrefab;
        [SerializeField, Min(1f)] float _pickRadiusPixels = 14f;

        readonly List<ShipAgent> _singleShip = new List<ShipAgent>(1);
        readonly List<int> _questSystems = new List<int>();
        readonly List<int> _questZones = new List<int>();
        readonly List<GameText> _recentNotices = new List<GameText>(MaximumRecentNotices);
        ShipSelectionInspection _shipInspection;
        RunZoneRegistry _runZones;
        float _lastClickTime = float.NegativeInfinity;
        int _lastClickedIndex = -1;
        ZoneView _lastZone;

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
        public FleetPopulation Population => _population;
        public FleetInventory Inventory => _rewards.Inventory;
        public int VisitedSystemCount => _runZones != null ? _runZones.VisitedSystemCount : 0;
        /// <summary>The latest notices, oldest first; several can arrive at once, e.g. a loss and its casualties.</summary>
        public IReadOnlyList<GameText> RecentNotices => _recentNotices;

        /// <summary>A run began: at the start, after R, or from the end screen.</summary>
        public event System.Action RunStarted;

        void Awake()
        {
            if (_galaxyMapView == null || _starSystemView == null || _zoneWorld == null || _environment == null ||
                _cameraRig == null || _shipRegistry == null || _shipInput == null || _groupInput == null ||
                _hangarInput == null || _mining == null || _scouting == null || _population == null || _docking == null || _search == null || _rewards == null || _combat == null || _pursuit == null || _proninAttack == null || _fleetJump == null || _jumpDialog == null || _jumpStatus == null || _inspection == null || _shipPanel == null || _quests == null ||
                _refineryPrefab == null || _tankerPrefab == null || _linerPrefab == null || _harvesterPrefab == null || _shuttlePrefab == null ||
                _cruiserPrefab == null || _fighterPrefab == null)
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
            _population.Notice += ShowNotice;
            if (_water != null)
            {
                _water.Notice += ShowNotice;
            }
            if (_evacuation != null)
            {
                _evacuation.Notice += ShowNotice;
            }
            if (_requests != null)
            {
                _requests.Notice += ShowNotice;
            }
            if (_repairs != null)
            {
                _repairs.Notice += ShowNotice;
            }
            if (_emp != null && _empTargeting != null)
            {
                _emp.Notice += ShowNotice;
                _empTargeting.Notice += ShowNotice;
            }
            if (_fuelReturn != null)
            {
                _fuelReturn.Notice += ShowNotice;
            }
            _docking.Notice += ShowNotice;
            _search.Notice += ShowNotice;
            _rewards.Notice += ShowNotice;
            _combat.Notice += ShowNotice;
            _pursuit.Notice += ShowNotice;
            _proninAttack.Notice += ShowNotice;
            _fleetJump.Notice += ShowNotice;
            _fleetJump.Jumped += OnFleetJumped;
            _quests.Notice += ShowNotice;
            _quests.MarkersChanged += RefreshQuestMarkers;
            _jumpStatus.JumpMapRequested += ShowGalaxyMap;
            _inspection.Notice += ShowNotice;
            _shipInput.DepositClicked += InspectDeposit;
            _shipInput.StationClicked += InspectStation;
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
            _shipInspection ??= new ShipSelectionInspection(Selection, _shipRegistry, _hangarInput, _repairs,
                _emp != null && _empTargeting != null ? new EmpInspection(_emp, _empTargeting) : null,
                _fuelReturn != null ? new FuelReturnInspection(_fuelReturn) : null);
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
            _population.Notice -= ShowNotice;
            if (_water != null)
            {
                _water.Notice -= ShowNotice;
            }
            if (_evacuation != null)
            {
                _evacuation.Notice -= ShowNotice;
            }
            if (_requests != null)
            {
                _requests.Notice -= ShowNotice;
            }
            if (_repairs != null)
            {
                _repairs.Notice -= ShowNotice;
            }
            if (_emp != null && _empTargeting != null)
            {
                _emp.Notice -= ShowNotice;
                _empTargeting.Notice -= ShowNotice;
            }
            if (_fuelReturn != null)
            {
                _fuelReturn.Notice -= ShowNotice;
            }
            _docking.Notice -= ShowNotice;
            _search.Notice -= ShowNotice;
            _rewards.Notice -= ShowNotice;
            _combat.Notice -= ShowNotice;
            _pursuit.Notice -= ShowNotice;
            _proninAttack.Notice -= ShowNotice;
            _fleetJump.Notice -= ShowNotice;
            _fleetJump.Jumped -= OnFleetJumped;
            _quests.Notice -= ShowNotice;
            _quests.MarkersChanged -= RefreshQuestMarkers;
            _jumpStatus.JumpMapRequested -= ShowGalaxyMap;
            _inspection.Notice -= ShowNotice;
            _shipInput.DepositClicked -= InspectDeposit;
            _shipInput.StationClicked -= InspectStation;
            _shipInput.EmptySpaceClicked -= _inspection.Clear;
            _shipRegistry.ShipLost -= OnShipLost;
            _shipRegistry.JumpRefusedForFuel -= OnJumpRefusedForFuel;
            Selection.Changed -= UpdateShipPanel;
        }

        void InspectDeposit(ZoneView zone, ResourceDeposit deposit)
        {
            _inspection.Show(new DepositInspection(zone, deposit, _mining, _scouting, () => Selection.Ships));
        }

        void InspectStation(ZoneView zone, ZoneStation station)
        {
            _inspection.Show(new StationInspection(zone, station, _scouting, _shipRegistry, _docking, _search, _evacuation, () => Selection.Ships));
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

        /// <summary>Adds a line to the notice feed, e.g. from developer tools of the preview scene.</summary>
        public void ShowNotice(GameText message)
        {
            if (_recentNotices.Count == MaximumRecentNotices)
            {
                _recentNotices.RemoveAt(0);
            }
            _recentNotices.Add(message);
        }

        void Start()
        {
            StartNewRun();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            // The jump warning takes the keyboard while it is open; the end screen ends all input but its button.
            if (keyboard == null || mouse == null || _jumpDialog.IsOpen || (_runEndWindow != null && _runEndWindow.IsOpen))
            {
                return;
            }
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                ShowNotice(GameText.Verbatim($"Language: {LanguageSelection.SelectNext()}"));
            }
            else if (keyboard.tKey.wasPressedThisFrame && _pursuit.Countdown != null)
            {
                // Developer shortcut: countdown and jump drive cooldown take minutes.
                _pursuit.SkipAhead(DeveloperPursuitSkipSeconds);
                _fleetJump.SkipAhead(DeveloperPursuitSkipSeconds);
                ShowNotice(GameText.Verbatim($"Pursuit countdown and jump drive skipped ahead by {DeveloperPursuitSkipSeconds:0} s."));
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
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ShowLastZone();
            }
            else if (WasEnterPressed(keyboard))
            {
                RequestJump(SelectedSystemIndex);
            }
            else if (mouse.leftButton.wasPressedThisFrame &&
                     _galaxyMapView.TryPickSystem(_cameraRig.Camera, mouse.position.ReadValue(), _pickRadiusPixels, out int systemIndex))
            {
                // A click only marks the target; range, lines and camera stay on the fleet's system.
                _galaxyMapView.Select(systemIndex);
                if (RegisterClick(systemIndex))
                {
                    RequestJump(systemIndex);
                }
            }
        }

        void HandleStarSystemInput(Keyboard keyboard, Mouse mouse)
        {
            Vector2 pointer = mouse.position.ReadValue();
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ShowGalaxyMap();
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
                ShowNotice(GenerationPreviewTexts.OrderedToZone.Format(("count", Selection.Count), ("zone", CurrentSystem.Zones[targetIndex].PointOfInterest.Name)));
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
            else if (keyboard.nKey.wasPressedThisFrame)
            {
                KillSelectedCrewMember();
            }
        }

        // Developer shortcut to try short-handed crews and derelicts without a radioactive rock.
        void KillSelectedCrewMember()
        {
            _singleShip.Clear();
            _singleShip.AddRange(Selection.Ships);
            int killed = 0;
            foreach (ShipAgent ship in _singleShip)
            {
                if (ship.Crew != null)
                {
                    killed += ship.Crew.Kill(1, DeveloperDamageCause);
                }
            }
            ShowNotice(GameText.Verbatim($"Killed {killed} crew member(s)."));
        }

        // Developer shortcut to try the out-of-fuel rules without flying a tank dry.
        void EmptySelectedTanks()
        {
            foreach (ShipAgent ship in Selection.Ships)
            {
                ship.Fuel.Tank.Drain(ship.Fuel.Tank.Amount);
            }
            ShowNotice(GameText.Verbatim($"Emptied the tanks of {Selection.Count} ship(s)."));
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
            ShowNotice(GameText.Verbatim($"Dealt {damage:0} damage to {_singleShip.Count} ship(s)."));
        }

        void OnShipLost(ShipAgent ship, GameText cause)
        {
            ShowNotice(ShipTexts.ShipDestroyed.Format(("ship", ship.DisplayName), ("cause", cause)));
        }

        void OnJumpRefusedForFuel(ShipAgent ship, int targetZoneIndex, float requiredFuel)
        {
            string target = _zoneWorld.CurrentSystem.Zones[targetZoneIndex].PointOfInterest.Name;
            ShowNotice(ShipTexts.JumpRefusedForFuel.Format(("ship", ship.DisplayName), ("zone", target), ("needed", requiredFuel), ("left", ship.Fuel.Tank.Amount)));
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
                ShowNotice(GameText.Verbatim($"Scanned all {CurrentZone.Record.Contents.Deposits.Count} deposits in this zone."));
            }
            else if (NearestDeposit != null)
            {
                ResourceDeposit deposit = NearestDeposit;
                deposit.Scan();
                ShowNotice(GameText.Verbatim($"Scanned: {DescribeDeposit(deposit)}."));
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

        /// <summary>A new run in a new galaxy, e.g. from the end screen.</summary>
        public void StartNextRun()
        {
            _galaxySeed++;
            StartNewRun();
        }

        void StartNewRun()
        {
            _runZones = new RunZoneRegistry();
            if (_evacuation != null)
            {
                _evacuation.Clear();
            }
            _zoneWorld.Clear();
            _starSystemView.Clear();
            _jumpDialog.Close();
            GalaxyLayout galaxy = GalaxyGenerator.Generate((ulong)_galaxySeed, _galaxySettings);
            _galaxyMapView.Show(galaxy);
            // Before the arrival, so the first quest step is in place when the start system is entered.
            _quests.BeginRun(galaxy, _runZones, new[] { MeltdownQuest.Create() });
            EnterStarSystem(galaxy.StartSystemIndex, null);
            ShowNotice(GameText.Verbatim($"New run with galaxy seed {_galaxySeed}; arrived at the jump point."));
            RunStarted?.Invoke();
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
            _cameraRig.SnapTo(_galaxyMapView.GetSystemWorldPosition(_galaxyMapView.CurrentSystemIndex), _galaxyMapView.ExtentUnits * MapViewDistanceFactor);
        }

        /// <summary>Back from the star map to the zone viewed last; the system map if that zone is gone.</summary>
        void ShowLastZone()
        {
            // Unity's overloaded equality treats a zone view destroyed with its system as null.
            if (_lastZone != null)
            {
                ShowZone(_lastZone);
                return;
            }
            ShowStarSystemMap();
        }

        /// <summary>
        /// Shows the jump warning for a system in range; the current system leads back to its map, and
        /// a jump that cannot start says why.
        /// </summary>
        void RequestJump(int systemIndex)
        {
            if (CurrentSystem != null && systemIndex == CurrentSystem.SystemIndex)
            {
                ShowStarSystemMap();
                return;
            }
            GameText blocked = _fleetJump.BlockedReason(systemIndex);
            if (blocked != null)
            {
                ShowNotice(blocked);
                return;
            }
            _jumpDialog.Open(systemIndex);
        }

        /// <summary>The galaxy map rings every system with a quest step waiting, the system map the zones to go to.</summary>
        void RefreshQuestMarkers()
        {
            _quests.CollectMarkedSystems(_questSystems);
            _galaxyMapView.SetHighlightedSystems(_questSystems);
            if (CurrentSystem != null)
            {
                _quests.CollectMarkedZones(CurrentSystem.SystemIndex, _questZones);
                _starSystemView.SetHighlightedPointsOfInterest(_questZones);
            }
        }

        /// <summary>The quest step waiting in a system, for the overlay; null if there is none.</summary>
        public GameText QuestMarkerOf(int systemIndex)
        {
            return _quests.TryGetMarker(systemIndex, out GameText marker) ? marker : null;
        }

        void OnFleetJumped(int systemIndex, IReadOnlyList<ShipAgent> jumpers)
        {
            EnterStarSystem(systemIndex, jumpers);
        }

        /// <summary>
        /// Shows a system with the fleet at its jump point: the starting fleet at the start of a run
        /// (arriving ships null), else the capital ships that jumped, which bring their hangars along.
        /// </summary>
        void EnterStarSystem(int systemIndex, IReadOnlyList<ShipAgent> arrivingShips)
        {
            _galaxyMapView.SetCurrentSystem(systemIndex);
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
            _quests.SystemEntered(system);
            RefreshQuestMarkers();
            int arrivalIndex = system.Layout.ArrivalPointOfInterestIndex;
            ZoneView arrival = _zoneWorld.Reveal(arrivalIndex);
            _starSystemView.SetMarkerStyle(arrivalIndex, PointOfInterestMarkerStyle.Explored);
            _starSystemView.SelectPointOfInterest(arrivalIndex);

            if (arrivingShips == null)
            {
                SpawnStartingFleet(arrival);
            }
            else
            {
                PlaceArrivingShips(arrival, arrivingShips);
            }

            ShowZone(arrival);
            ShowNotice(GenerationPreviewTexts.ArrivedInSystem.Format(("system", system.Layout.Name), ("known", system.RevealedCount)));

            // Every arrival starts a new countdown and the jump drive's cooldown, also on a return to a visited system.
            _pursuit.Begin();
            _fleetJump.ArrivedIn(Galaxy, systemIndex);
        }

        /// <summary>
        /// The run's starting fleet: a liner with a shuttle squad, a refinery with two harvester squads,
        /// a tanker and a cruiser with two fighter squads in their hangars.
        /// </summary>
        void SpawnStartingFleet(ZoneView arrival)
        {
            ShipAgent cruiser = _shipRegistry.Spawn(_cruiserPrefab, arrival, CruiserArrivalPosition, 0f);
            Hangar fighterHangar = cruiser.GetComponent<Hangar>();
            _shipRegistry.SpawnSquadDocked(_fighterPrefab, fighterHangar, FightersPerSquad);
            _shipRegistry.SpawnSquadDocked(_fighterPrefab, fighterHangar, FightersPerSquad);
            ShipAgent liner = _shipRegistry.Spawn(_linerPrefab, arrival, LinerArrivalPosition, 0f);
            _shipRegistry.SpawnSquadDocked(_shuttlePrefab, liner.GetComponent<Hangar>(), ShuttlesPerSquad);
            ShipAgent refinery = _shipRegistry.Spawn(_refineryPrefab, arrival, RefineryArrivalPosition, 0f);
            Hangar hangar = refinery.GetComponent<Hangar>();
            Squad firstSquad = _shipRegistry.SpawnSquadDocked(_harvesterPrefab, hangar, HarvestersPerSquad);
            _shipRegistry.SpawnSquadDocked(_harvesterPrefab, hangar, HarvestersPerSquad);
            _shipRegistry.Spawn(_tankerPrefab, arrival, TankerArrivalPosition, 0f);
            _shipInput.SelectSquad(firstSquad, false);
        }

        /// <summary>Capital ships out of a fleet jump, side by side in rows around the arrival point.</summary>
        static void PlaceArrivingShips(ZoneView arrival, IReadOnlyList<ShipAgent> ships)
        {
            for (int i = 0; i < ships.Count; i++)
            {
                int row = i / ArrivalShipsPerRow;
                int column = i % ArrivalShipsPerRow;
                Vector3 position = new Vector3((column - (ArrivalShipsPerRow - 1) * 0.5f) * ArrivalSpacingUnits, 0f, -row * ArrivalSpacingUnits);
                ships[i].ArriveAfterFleetJump(arrival, position, 0f);
            }
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
                ShowNotice(GenerationPreviewTexts.ZoneAlreadyRevealed.Format(("zone", CurrentSystem.Zones[pointOfInterestIndex].PointOfInterest.Name)));
                return;
            }
            _zoneWorld.Reveal(pointOfInterestIndex);
        }

        /// <summary>The view of a revealed zone of the current system.</summary>
        public bool TryGetZoneView(int pointOfInterestIndex, out ZoneView view)
        {
            return _zoneWorld.TryGetView(pointOfInterestIndex, out view);
        }

        void ViewZone(int pointOfInterestIndex)
        {
            if (!_zoneWorld.TryGetView(pointOfInterestIndex, out ZoneView view))
            {
                ShowNotice(GenerationPreviewTexts.ZoneUnexplored);
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
            _lastZone = view;
            _galaxyMapView.SetVisible(false);
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
                ShowNotice(GenerationPreviewTexts.NoShipSelected);
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
            ShowNotice(GenerationPreviewTexts.ZoneRevealed.Format(("zone", view.Record.PointOfInterest.Name)));
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
                ShowNotice(GameText.Verbatim("No deposits left in this zone."));
                return;
            }

            ResourceDeposit deposit = CurrentZone.Record.Contents.Deposits[index];
            float extracted = deposit.Extract(deposit.InitialAmount * DeveloperExtractFraction);
            CurrentZone.Refresh();
            ShowNotice(GameText.Verbatim($"Extracted {extracted:0} {deposit.Resource}; {deposit.RemainingAmount:0} of {deposit.InitialAmount:0} left."));
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
