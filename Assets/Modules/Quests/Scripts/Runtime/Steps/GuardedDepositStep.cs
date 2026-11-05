using System;
using LastFleet.Dialogue;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// Take something from a guarded rock: a rock lies next to a station that defends it. Hailing the
    /// station gets a warning; the moment a harvester mines the rock, the station's guards launch and go
    /// for the fleet ships near it. After enough mining the item is aboard the miner. The step is done
    /// when that ship is safe, landed in a hangar or out of the zone. If it is lost first, so is the item,
    /// and the rock has to be mined again; the step only fails if the rock is mined out without the item.
    /// </summary>
    public sealed class GuardedDepositStep : IQuestStep
    {
        const ulong SiteSalt = 1UL;
        const ulong RockAngleSalt = 2UL;

        readonly GuardedDepositSettings _settings;
        QuestStepContext _context;
        QuestStationSite _stationSite;
        QuestDepositSite _rockSite;
        QuestStationContact _contact;
        DepositGuards _guards;
        ShipAgent _miner;
        ShipAgent _carrier;
        float _miningSeconds;
        bool _guardsLaunchedThisVisit;
        bool _isProvoked;

        public GuardedDepositStep(GuardedDepositSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public GameText Status
        {
            get
            {
                if (_carrier != null)
                {
                    return QuestTexts.ItemEscapeStatus.Format(("item", _settings.Item.Name), ("ship", _carrier.DisplayName));
                }
                if (_miningSeconds > 0f)
                {
                    return QuestTexts.MiningItemStatus.Format(("item", _settings.Item.Name),
                        ("percent", Mathf.FloorToInt(100f * _miningSeconds / _settings.MiningSecondsForItem)));
                }
                return QuestTexts.MineRockStatus.Format(("station", _settings.Station.Name));
            }
        }

        public void Begin(QuestStepContext context)
        {
            _context = context;
            _contact = new QuestStationContact(context.Services.Dialogue, HailConversation);
            _guards = new DepositGuards(context.Services.Fleet, _settings.GuardRadiusUnits);
            context.Cargo.Lost += OnItemLost;
        }

        public void OnSystemEntered(SystemZones system)
        {
            _guardsLaunchedThisVisit = false;
            if (_stationSite == null)
            {
                _stationSite = QuestStationSite.Place(system, _settings.Station, _context.CreateRandom(SiteSalt), _context.Services.ZoneWorld,
                    station => station.SetContact(_contact));
                float rockAngle = _context.CreateRandom(RockAngleSalt).AngleDegrees();
                _rockSite = QuestDepositSite.PlaceNextTo(_stationSite, system, _settings.RockResource, _settings.RockRadiusUnits,
                    _settings.RockAmount, rockAngle, _context.Services.ZoneWorld, null);
            }
            _context.MarkZone(_stationSite.PointOfInterestIndex);
        }

        public void OnSystemLeft()
        {
            _miner = null;
            // A carrier that left with the fleet sits in a hangar; the next tick sees it is safe.
        }

        public void Tick(float deltaSeconds)
        {
            // A ship without crew cannot fly the item to safety; for the quest it is lost with it.
            if (_carrier != null && _carrier.IsDerelict)
            {
                ShipAgent crewless = _carrier;
                _context.Cargo.Unload(_settings.Item);
                ForgetItem(crewless);
            }

            ResourceDeposit rock = _rockSite?.Deposit;
            if (rock == null || !_context.TryGetZoneView(_stationSite.PointOfInterestIndex, out ZoneView zone))
            {
                CheckCarrierSafe(null);
                return;
            }

            if (_carrier != null)
            {
                _guards.Tick(deltaSeconds, _carrier);
                CheckCarrierSafe(zone);
                return;
            }

            _miner = FindMiner(rock);
            if (_miner != null)
            {
                if (!_guardsLaunchedThisVisit && _guards.AliveCount == 0)
                {
                    LaunchGuards(zone, rock);
                }
                _miningSeconds += deltaSeconds;
                if (_miningSeconds >= _settings.MiningSecondsForItem)
                {
                    LoadItem(_miner);
                }
            }
            else if (rock.IsDepleted)
            {
                _context.Fail(QuestTexts.RockMinedOut.Format(("item", _settings.Item.Name)));
                return;
            }
            _guards.Tick(deltaSeconds, _miner);
        }

        public void End()
        {
            if (_context != null)
            {
                _context.Cargo.Lost -= OnItemLost;
            }
            _guards?.StandDown();
            _contact?.FallSilent();
            if (_stationSite?.Station != null && _stationSite.Station.Contact == _contact)
            {
                _stationSite.Station.SetContact(null);
            }
        }

        DialogueScript HailConversation()
        {
            return _isProvoked ? _settings.Outrage(_stationSite.Station) : _settings.Warning(_stationSite.Station);
        }

        /// <summary>A fleet harvester mining the rock right now.</summary>
        ShipAgent FindMiner(ResourceDeposit rock)
        {
            foreach (ShipAgent ship in _context.Services.Fleet.Ships)
            {
                if (!ship.IsDerelict && ship.TryGetComponent(out HarvesterMining mining) && mining.State == MiningState.Extracting && mining.Deposit == rock)
                {
                    return ship;
                }
            }
            return null;
        }

        void LaunchGuards(ZoneView zone, ResourceDeposit rock)
        {
            _guardsLaunchedThisVisit = true;
            bool firstTime = !_isProvoked;
            _isProvoked = true;
            if (_settings.GuardCount == 0)
            {
                return;
            }
            QuestServices services = _context.Services;
            _guards.Launch(services.Pronin, services.Prefabs.GuardFighter, _settings.GuardCount, zone, _stationSite.Station, rock.LocalPosition, _miner);
            _context.Notify(QuestTexts.GuardsLaunched.Format(("count", _settings.GuardCount), ("station", _stationSite.Station.Name)));
            if (firstTime)
            {
                _context.Say(_settings.Outrage(_stationSite.Station));
            }
        }

        void LoadItem(ShipAgent miner)
        {
            _context.Cargo.Load(_settings.Item, miner);
            _carrier = miner;
            _context.Notify(QuestTexts.ItemAboard.Format(("item", _settings.Item.Name), ("ship", miner.DisplayName)));
        }

        /// <summary>Done once the carrier has landed in a hangar or flies outside the rock's zone.</summary>
        void CheckCarrierSafe(ZoneView rockZone)
        {
            if (_carrier == null || _context.IsOver)
            {
                return;
            }
            bool landed = _carrier.IsDocked;
            bool away = rockZone == null || _carrier.Zone != rockZone;
            if (!landed && !away)
            {
                return;
            }
            _context.Notify(QuestTexts.ItemSecured.Format(("item", _settings.Item.Name), ("ship", _carrier.DisplayName)));
            if (_settings.Secured != null)
            {
                _context.Say(_settings.Secured(_carrier));
            }
            _context.Complete();
        }

        /// <summary>The item went down with its ship: it has to be mined again.</summary>
        void OnItemLost(QuestItem item, ShipAgent ship)
        {
            if (item == _settings.Item && !_context.IsOver)
            {
                ForgetItem(ship);
            }
        }

        void ForgetItem(ShipAgent ship)
        {
            _carrier = null;
            _miningSeconds = 0f;
            _context.Notify(QuestTexts.ItemLostRetry.Format(("item", _settings.Item.Name), ("ship", ship.DisplayName)));
        }
    }
}
