using System;
using System.Collections.Generic;
using LastFleet.Dialogue;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// Have a station work on a quest item: the ship carrying the item docks at the station and hands it
    /// over; the work only goes on while at least one fleet ship is docked (it pauses, keeping its progress,
    /// when all have left). The station is radioactive: docked crews build up a dose and must undock now
    /// and then (<see cref="DockedRadiation"/>). When the work is done, the result boards a docked ship,
    /// and the step is done once that ship lands in a hangar. Losing either item fails the step.
    /// </summary>
    public sealed class StationProcessingStep : IQuestStep
    {
        const ulong SiteSalt = 1UL;

        readonly StationProcessingSettings _settings;
        readonly List<ShipAgent> _docked = new List<ShipAgent>();
        QuestStepContext _context;
        QuestStationSite _site;
        QuestStationContact _contact;
        DockedRadiation _radiation;
        QuestTimer _work;
        ShipAgent _outputCarrier;

        public StationProcessingStep(StationProcessingSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public GameText Status
        {
            get
            {
                if (_outputCarrier != null)
                {
                    return QuestTexts.ItemHomeStatus.Format(("item", _settings.Output.Name), ("ship", _outputCarrier.DisplayName));
                }
                if (_work != null && _work.HasRunOut)
                {
                    return QuestTexts.CollectItemStatus.Format(("station", _settings.Station.Name), ("item", _settings.Output.Name));
                }
                if (_work != null)
                {
                    return QuestTexts.ProcessingStatus.Format(("remaining", ClockFormat.MinutesSeconds(_work.RemainingSeconds)),
                        ("docked", CountCrewedDocked()), ("dose", Mathf.RoundToInt(100f * _radiation.HighestDockedLevel)));
                }
                if (_context != null && _context.Cargo.TryGetCarrier(_settings.Input, out ShipAgent carrier))
                {
                    return QuestTexts.DeliverItemStatus.Format(("item", _settings.Input.Name), ("ship", carrier.DisplayName),
                        ("station", _settings.Station.Name));
                }
                return null;
            }
        }

        public void Begin(QuestStepContext context)
        {
            _context = context;
            _contact = new QuestStationContact(context.Services.Dialogue, () => _settings.Briefing(_site.Station));
            _radiation = new DockedRadiation(_settings.CreateDose, _settings.RadiationWarningLevel, QuestTexts.RadiationCause);
            context.Cargo.Lost += OnItemLost;
            if (!context.Cargo.TryGetCarrier(_settings.Input, out ShipAgent _))
            {
                context.Fail(QuestTexts.ItemMissing.Format(("item", _settings.Input.Name)));
            }
        }

        public void OnSystemEntered(SystemZones system)
        {
            _site ??= QuestStationSite.Place(system, _settings.Station, _context.CreateRandom(SiteSalt), _context.Services.ZoneWorld, station =>
            {
                station.SetContact(_contact);
                station.UseScriptedRadiation();
            });
            _context.MarkZone(_site.PointOfInterestIndex);
        }

        public void OnSystemLeft()
        {
            _docked.Clear();
        }

        public void Tick(float deltaSeconds)
        {
            CollectDocked();
            _radiation.Tick(deltaSeconds, _docked, WarnRadiation);

            if (CheckCrewlessCarriers())
            {
                return;
            }

            if (_work == null)
            {
                TryReceiveInput();
            }
            else if (!_work.HasRunOut && CountCrewedDocked() > 0 && _work.Advance(deltaSeconds))
            {
                _context.Notify(QuestTexts.ProcessingDone.Format(("item", _settings.Output.Name), ("station", _site.Station.Name)));
            }

            if (_work != null && _work.HasRunOut && _outputCarrier == null)
            {
                TryHandOutOutput();
            }
            if (_outputCarrier != null && _outputCarrier.IsDocked)
            {
                _context.Notify(QuestTexts.ItemHome.Format(("item", _settings.Output.Name), ("ship", _outputCarrier.DisplayName)));
                _context.Complete();
            }
        }

        public void End()
        {
            if (_context != null)
            {
                _context.Cargo.Lost -= OnItemLost;
            }
            _contact?.FallSilent();
            if (_site?.Station != null && _site.Station.Contact == _contact)
            {
                _site.Station.SetContact(null);
            }
        }

        /// <summary>Fleet ships docked at the station right now.</summary>
        void CollectDocked()
        {
            _docked.Clear();
            ZoneStation station = _site?.Station;
            if (station == null || !_context.TryGetZoneView(_site.PointOfInterestIndex, out ZoneView _))
            {
                return;
            }
            foreach (ShipAgent ship in _context.Services.Fleet.Ships)
            {
                if (ship.TryGetComponent(out StationDocking docking) && docking.State == DockingState.Docked && docking.Station == station)
                {
                    _docked.Add(ship);
                }
            }
        }

        /// <summary>
        /// A ship whose crew died cannot fly its item anywhere. Docked at the station, the station keeps the
        /// result for the next ship; anywhere else the item is as good as lost. Returns true if the step failed.
        /// </summary>
        bool CheckCrewlessCarriers()
        {
            if (_outputCarrier != null && _outputCarrier.IsDerelict)
            {
                ShipAgent crewless = _outputCarrier;
                _context.Cargo.Unload(_settings.Output);
                _outputCarrier = null;
                if (_docked.Contains(crewless))
                {
                    _context.Notify(QuestTexts.ItemReturned.Format(("ship", crewless.DisplayName), ("station", _site.Station.Name),
                        ("item", _settings.Output.Name)));
                    return false;
                }
                _context.Fail(QuestTexts.ItemCrewLost.Format(("ship", crewless.DisplayName), ("item", _settings.Output.Name)));
                return true;
            }

            if (_work == null && _context.Cargo.TryGetCarrier(_settings.Input, out ShipAgent carrier) && carrier.IsDerelict &&
                !_docked.Contains(carrier))
            {
                _context.Cargo.Unload(_settings.Input);
                _context.Fail(QuestTexts.ItemCrewLost.Format(("ship", carrier.DisplayName), ("item", _settings.Input.Name)));
                return true;
            }
            return false;
        }

        /// <summary>The station only works while someone alive is docked.</summary>
        int CountCrewedDocked()
        {
            int crewed = 0;
            foreach (ShipAgent ship in _docked)
            {
                if (!ship.IsDerelict)
                {
                    crewed++;
                }
            }
            return crewed;
        }

        void TryReceiveInput()
        {
            foreach (ShipAgent ship in _docked)
            {
                if (!_context.Cargo.IsCarriedBy(_settings.Input, ship))
                {
                    continue;
                }
                _context.Cargo.Unload(_settings.Input);
                _work = new QuestTimer(_settings.ProcessingSeconds);
                _context.Notify(QuestTexts.ItemHandedOver.Format(("item", _settings.Input.Name), ("ship", ship.DisplayName),
                    ("station", _site.Station.Name)));
                if (_settings.Received != null)
                {
                    _context.Say(_settings.Received(_site.Station, ship));
                }
                return;
            }
        }

        /// <summary>The result boards the docked ship with the lowest dose that still has a crew.</summary>
        void TryHandOutOutput()
        {
            ShipAgent best = null;
            foreach (ShipAgent ship in _docked)
            {
                if (!ship.IsDerelict && (best == null || _radiation.LevelOf(ship) < _radiation.LevelOf(best)))
                {
                    best = ship;
                }
            }
            if (best == null)
            {
                return;
            }
            _context.Cargo.Load(_settings.Output, best);
            _outputCarrier = best;
            _contact.FallSilent();
            _context.Notify(QuestTexts.ItemAboard.Format(("item", _settings.Output.Name), ("ship", best.DisplayName)));
            if (_settings.Finished != null)
            {
                _context.Say(_settings.Finished(_site.Station, best));
            }
        }

        void WarnRadiation(ShipAgent ship)
        {
            _context.Notify(QuestTexts.RadiationWarning.Format(("ship", ship.DisplayName), ("station", _site.Station.Name)));
        }

        void OnItemLost(QuestItem item, ShipAgent ship)
        {
            if (item == _settings.Input || item == _settings.Output)
            {
                _context.Fail(QuestTexts.ItemLost.Format(("item", item.Name), ("ship", ship.DisplayName)));
            }
        }
    }
}
