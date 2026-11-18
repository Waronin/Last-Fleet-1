using LastFleet.AbandonedShips;
using LastFleet.People;
using LastFleet.Provisions;
using LastFleet.Quests;
using LastFleet.RunEnd;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.StrandedShips;
using UnityEngine;

namespace LastFleet.GenerationPreview
{
    /// <summary>
    /// Counts what happens during a run for the end screen's account: people rescued and lost, ships gained and
    /// lost, clues found. Starts over with every run. Optional services are skipped when missing.
    /// </summary>
    public sealed class RunTallyRecorder : MonoBehaviour
    {
        [SerializeField] GenerationPreviewController _controller;
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] FleetPopulation _population;
        [SerializeField] QuestDirector _quests;
        [Tooltip("Optional.")]
        [SerializeField] FleetWaterSupply _water;
        [Tooltip("Optional.")]
        [SerializeField] StrandedShipRecovery _strandedShips;
        [Tooltip("Optional.")]
        [SerializeField] CrewTransferService _crewTransfer;

        readonly RunTally _tally = new RunTally();

        /// <summary>The counts of the current run; take a copy to keep them.</summary>
        public RunTally Tally => _tally;

        void Awake()
        {
            if (_controller == null || _fleet == null || _population == null || _quests == null)
            {
                Debug.LogError("RunTallyRecorder needs the preview controller, the fleet's registry, the population and the quest director.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _controller.RunStarted += _tally.Reset;
            _fleet.ShipLost += OnShipLost;
            _population.PeopleRescued += _tally.AddRescued;
            _population.PeopleDied += _tally.AddPeopleLost;
            _quests.StepCompleted += _tally.AddClue;
            if (_water != null)
            {
                _water.PassengersDied += _tally.AddPeopleLost;
            }
            if (_strandedShips != null)
            {
                _strandedShips.ShipRecovered += OnShipRecovered;
            }
            if (_crewTransfer != null)
            {
                _crewTransfer.ShipJoined += OnShipJoined;
            }
        }

        void OnDisable()
        {
            _controller.RunStarted -= _tally.Reset;
            _fleet.ShipLost -= OnShipLost;
            _population.PeopleRescued -= _tally.AddRescued;
            _population.PeopleDied -= _tally.AddPeopleLost;
            _quests.StepCompleted -= _tally.AddClue;
            if (_water != null)
            {
                _water.PassengersDied -= _tally.AddPeopleLost;
            }
            if (_strandedShips != null)
            {
                _strandedShips.ShipRecovered -= OnShipRecovered;
            }
            if (_crewTransfer != null)
            {
                _crewTransfer.ShipJoined -= OnShipJoined;
            }
        }

        void OnShipLost(ShipAgent ship, GameText cause)
        {
            _tally.AddShipLost(ship.IsCapital);
            if (!ship.TryGetComponent(out Hangar hangar))
            {
                return;
            }
            // Ships docked in a destroyed carrier go down with it without a loss of their own. Ships the registry
            // abandons with their carrier were reported and unregistered first, so they are not counted twice.
            foreach (ShipAgent docked in hangar.DockedShips)
            {
                if (IsInFleet(docked))
                {
                    _tally.AddShipLost(docked.IsCapital);
                }
            }
        }

        bool IsInFleet(ShipAgent ship)
        {
            foreach (ShipAgent member in _fleet.Ships)
            {
                if (member == ship)
                {
                    return true;
                }
            }
            return false;
        }

        void OnShipRecovered(ShipAgent ship, int survivors)
        {
            _tally.AddShipGained(ship.IsCapital);
            _tally.AddRescued(survivors);
        }

        void OnShipJoined(ShipAgent ship)
        {
            _tally.AddShipGained(ship.IsCapital);
        }
    }
}
