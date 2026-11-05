using System;
using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// Set off a supernova: first the generator station is held like any processing station (its fuel
    /// handed over, a crewed ship docked until the work is done). Then the star collapses on a countdown.
    /// The step is done when the fleet jumps out of the system in time; if the countdown runs out with the
    /// fleet still there, the fleet goes down with the star and the run is lost.
    /// </summary>
    public sealed class SupernovaStep : IQuestStep
    {
        readonly SupernovaSettings _settings;
        readonly StationProcessingStep _hold;
        QuestStepContext _context;
        QuestStepContext _holdContext;
        QuestTimer _countdown;
        string _systemName = string.Empty;
        bool _isHoldOver;

        public SupernovaStep(SupernovaSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _hold = new StationProcessingStep(settings.Hold);
        }

        public GameText Status => _countdown != null
            ? QuestTexts.SupernovaStatus.Format(("remaining", ClockFormat.MinutesSeconds(_countdown.RemainingSeconds)),
                ("system", GameText.Verbatim(_systemName)))
            : _hold.Status;

        public void Begin(QuestStepContext context)
        {
            _context = context;
            // The hold reports to this step, which decides what its completion means.
            _holdContext = new QuestStepContext(context.Chain, context.Services, context.Cargo, context.Seed, _ => Ignite(),
                (_, reason) => _context.Fail(reason), _context.Notify, hold => _context.MarkZone(hold.MarkedZoneIndex), _context.LoseRun);
            _hold.Begin(_holdContext);
        }

        public void OnSystemEntered(SystemZones system)
        {
            _systemName = system.Layout.Name;
            if (!_isHoldOver)
            {
                _hold.OnSystemEntered(system);
            }
        }

        public void OnSystemLeft()
        {
            if (_countdown == null)
            {
                _hold.OnSystemLeft();
                return;
            }

            _context.Notify(QuestTexts.SupernovaEscaped.Format(("system", GameText.Verbatim(_systemName))));
            if (_settings.Escaped != null)
            {
                _context.Say(_settings.Escaped(_systemName));
            }
            _context.Complete();
        }

        public void Tick(float deltaSeconds)
        {
            if (_countdown == null)
            {
                if (!_isHoldOver)
                {
                    _hold.Tick(deltaSeconds);
                }
                return;
            }
            if (_countdown.Advance(deltaSeconds))
            {
                _context.LoseRun(QuestTexts.SupernovaCaught.Format(("system", GameText.Verbatim(_systemName))));
            }
        }

        public void End()
        {
            EndHold();
        }

        void Ignite()
        {
            EndHold();
            _countdown = new QuestTimer(_settings.CountdownSeconds);
            _context.Notify(QuestTexts.SupernovaIgnited.Format(("system", GameText.Verbatim(_systemName)),
                ("remaining", ClockFormat.MinutesSeconds(_settings.CountdownSeconds))));
            if (_settings.Ignited != null)
            {
                _context.Say(_settings.Ignited());
            }
        }

        void EndHold()
        {
            if (_isHoldOver || _holdContext == null)
            {
                return;
            }
            _isHoldOver = true;
            _hold.End();
        }
    }
}
