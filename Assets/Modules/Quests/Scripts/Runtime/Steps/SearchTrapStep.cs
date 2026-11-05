using System;
using LastFleet.Combat;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// A station that blows up when searched: instead of the first find a fuse starts, the search stops
    /// and the player has that long to get the ships out of the zone. When it goes off, every ship left in
    /// the zone, enemies included, takes the blast; the station is gone and leaves salvageable wrecks.
    /// The step is done with the explosion; whatever the trap revealed came by radio when it went off.
    /// </summary>
    public sealed class SearchTrapStep : IQuestStep, IStationFindInterceptor
    {
        const ulong SiteSalt = 1UL;
        const ulong WreckSalt = 2UL;
        const float ExplosionSizePerFootprintUnit = 1.5f;

        readonly SearchTrapSettings _settings;
        QuestStepContext _context;
        QuestStationSite _site;
        QuestTimer _fuse;

        public SearchTrapStep(SearchTrapSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public GameText Status => _fuse != null && !_fuse.HasRunOut
            ? QuestTexts.FuseStatus.Format(("remaining", ClockFormat.MinutesSeconds(_fuse.RemainingSeconds)))
            : null;

        public void Begin(QuestStepContext context)
        {
            _context = context;
            context.Services.Search.AddInterceptor(this);
        }

        public void OnSystemEntered(SystemZones system)
        {
            _site ??= QuestStationSite.Place(system, _settings.Station, _context.CreateRandom(SiteSalt), _context.Services.ZoneWorld, null);
            _context.MarkZone(_site.PointOfInterestIndex);
        }

        public void OnSystemLeft()
        {
            // The fleet cannot jump out within the fuse, but if it did, the station still goes up.
            if (_fuse != null && _site?.Station != null)
            {
                _site.Station.MarkDestroyed();
                _context.Complete();
            }
        }

        public bool TryIntercept(ZoneStation station, ShipAgent finder)
        {
            if (_fuse != null || _site == null || station != _site.Station)
            {
                return false;
            }

            _fuse = new QuestTimer(_settings.FuseSeconds);
            _context.Notify(QuestTexts.TrapTriggered.Format(("station", station.Name)));
            _context.Say(_settings.TriggeredMessage(station));
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (_fuse != null && _fuse.Advance(deltaSeconds))
            {
                Explode();
            }
        }

        public void End()
        {
            _context?.Services.Search.RemoveInterceptor(this);
        }

        void Explode()
        {
            ZoneStation station = _site.Station;
            QuestServices services = _context.Services;
            if (!services.ZoneWorld.TryGetView(_site.PointOfInterestIndex, out ZoneView zone))
            {
                station.MarkDestroyed();
                _context.Complete();
                return;
            }

            Vector3 center = zone.transform.TransformPoint(station.LocalPosition);
            ExplosionEffect explosion = UnityEngine.Object.Instantiate(services.Prefabs.Explosion, center, Quaternion.identity, zone.transform);
            explosion.Play(station.FootprintRadiusUnits * ExplosionSizePerFootprintUnit);

            station.MarkDestroyed();
            zone.RemoveStationView(station);
            LeaveWrecks(zone, station);

            int hit = services.Fleet.DamageShipsInZone(zone, _settings.BlastDamage, QuestTexts.StationExplosionCause)
                      + services.Pronin.DamageShipsInZone(zone, _settings.BlastDamage, QuestTexts.StationExplosionCause);
            _context.Notify(QuestTexts.StationExploded.Format(("station", station.Name), ("count", hit)));
            _context.Complete();
        }

        void LeaveWrecks(ZoneView zone, ZoneStation station)
        {
            DeterministicRandom random = _context.CreateRandom(WreckSalt);
            for (int i = 0; i < _settings.WreckCount; i++)
            {
                float distance = random.Range(0.2f, 0.8f) * station.FootprintRadiusUnits;
                float angle = random.Range(0f, Mathf.PI * 2f);
                Vector3 position = station.LocalPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                zone.AddDeposit(ResourceDeposit.Wreck(position, _settings.MetalPerWreck, random.AngleDegrees()));
            }
        }
    }
}
