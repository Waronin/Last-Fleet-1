using System;
using LastFleet.Combat;
using LastFleet.Dialogue;
using LastFleet.People;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// Bring a ship home safely: hailing the station offers the escort; once accepted, a shuttle with
    /// its people launches and flies to a hangar of the fleet on its own, and Pronin fighters go for it.
    /// The shuttle belongs to the fleet from the start, so the player can steer it and escort it with
    /// fighters. The step is done when it lands, and fails if it is lost on the way, e.g. destroyed or
    /// left behind at a fleet jump.
    /// </summary>
    public sealed class EscortStep : IQuestStep
    {
        const ulong SiteSalt = 1UL;
        const ulong AttackSalt = 2UL;
        const float LaunchClearanceUnits = 6f;
        const float AttackerArrivalDistanceFraction = 0.6f;

        readonly EscortSettings _settings;
        QuestStepContext _context;
        QuestStationSite _site;
        QuestStationContact _contact;
        ShipAgent _shuttle;
        QuestTimer _attackDelay;

        public EscortStep(EscortSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public GameText Status
        {
            get
            {
                if (_shuttle != null)
                {
                    return QuestTexts.EscortUnderwayStatus.Format(("ship", _shuttle.DisplayName));
                }
                return QuestTexts.EscortHailStatus.Format(("station", _settings.Station.Name));
            }
        }

        public void Begin(QuestStepContext context)
        {
            _context = context;
            _contact = new QuestStationContact(context.Services.Dialogue, OfferConversation);
            context.Services.Fleet.ShipRemoved += OnShipRemoved;
        }

        public void OnSystemEntered(SystemZones system)
        {
            _site ??= QuestStationSite.Place(system, _settings.Station, _context.CreateRandom(SiteSalt), _context.Services.ZoneWorld,
                station => station.SetContact(_contact));
            _context.MarkZone(_site.PointOfInterestIndex);
        }

        public void OnSystemLeft()
        {
            // A shuttle still out in space is left behind by the jump and reported lost.
        }

        public void Tick(float deltaSeconds)
        {
            if (_attackDelay != null && _attackDelay.Advance(deltaSeconds))
            {
                LaunchAttack();
            }
        }

        public void End()
        {
            if (_context != null)
            {
                _context.Services.Fleet.ShipRemoved -= OnShipRemoved;
            }
            if (_shuttle != null)
            {
                _shuttle.DockedInHangar -= OnShuttleLanded;
            }
            _contact?.FallSilent();
            if (_site?.Station != null && _site.Station.Contact == _contact)
            {
                _site.Station.SetContact(null);
            }
        }

        DialogueScript OfferConversation()
        {
            return _settings.Offer(_site.Station, Accept);
        }

        /// <summary>The player accepted: the shuttle launches from the station and heads for the fleet.</summary>
        void Accept()
        {
            if (_shuttle != null || _context.IsOver || _site?.Station == null ||
                !_context.Services.ZoneWorld.TryGetView(_site.PointOfInterestIndex, out ZoneView zone))
            {
                return;
            }

            ZoneStation station = _site.Station;
            QuestServices services = _context.Services;
            Vector2 facing = ShipMotion.HeadingToDirection(station.HeadingDegrees);
            Vector3 outward = new Vector3(facing.x, 0f, facing.y);
            Vector3 position = station.LocalPosition + outward * (station.FootprintRadiusUnits + LaunchClearanceUnits);
            _shuttle = services.Fleet.Spawn(services.Prefabs.Shuttle, zone, position, ShipMotion.DirectionToHeading(facing));
            if (_settings.Passengers > 0 && _shuttle.TryGetComponent(out ShipPassengers seats))
            {
                seats.Quarters.Add(_settings.Passengers);
            }
            _shuttle.DockedInHangar += OnShuttleLanded;

            Hangar hangar = services.Fleet.FindHangarFor(_shuttle);
            if (hangar != null)
            {
                _shuttle.Order(ShipOrder.DockAt(hangar), false);
            }
            _contact.FallSilent();
            if (_settings.AttackerCount > 0)
            {
                _attackDelay = new QuestTimer(Mathf.Max(0.01f, _settings.AttackDelaySeconds));
            }
            _context.Notify(QuestTexts.EscortLaunched.Format(("ship", _shuttle.DisplayName), ("station", station.Name)));
        }

        /// <summary>Pronin fighters jump in at the edge of the shuttle's zone and go for it.</summary>
        void LaunchAttack()
        {
            if (_shuttle == null || _shuttle.IsDocked || _shuttle.Zone == null)
            {
                return;
            }

            ZoneView zone = _shuttle.Zone;
            DeterministicRandom random = _context.CreateRandom(AttackSalt);
            float angle = random.Range(0f, Mathf.PI * 2f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector3 position = new Vector3(direction.x, 0f, direction.y) * (zone.Zone.RadiusUnits * AttackerArrivalDistanceFraction);
            Squad raid = _context.Services.Pronin.SpawnSquad(_context.Services.Prefabs.ProninFighter, zone, position,
                ShipMotion.DirectionToHeading(-direction), _settings.AttackerCount);
            foreach (ShipAgent fighter in raid.Members)
            {
                if (fighter.TryGetComponent(out FighterCombat combat))
                {
                    combat.Attack(_shuttle);
                }
            }
            _context.Notify(QuestTexts.EscortAttacked.Format(("count", _settings.AttackerCount), ("ship", _shuttle.DisplayName)));
        }

        void OnShuttleLanded(ShipAgent ship, Hangar hangar)
        {
            _context.Notify(QuestTexts.EscortArrived.Format(("ship", ship.DisplayName), ("carrier", hangar.Carrier.DisplayName)));
            if (_settings.ArrivedMessage != null)
            {
                _context.Say(_settings.ArrivedMessage(ship));
            }
            _context.Complete();
        }

        void OnShipRemoved(ShipAgent ship)
        {
            if (ship == _shuttle && _shuttle != null)
            {
                _context.Fail(QuestTexts.EscortLost.Format(("ship", ship.DisplayName)));
            }
        }
    }
}
