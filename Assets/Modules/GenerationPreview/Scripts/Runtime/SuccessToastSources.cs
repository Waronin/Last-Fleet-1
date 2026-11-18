using LastFleet.AbandonedShips;
using LastFleet.Combat;
using LastFleet.Hud;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Provisions;
using LastFleet.Rewards;
using LastFleet.StrandedShips;
using UnityEngine;

namespace LastFleet.GenerationPreview
{
    /// <summary>
    /// Connects the services that report small wins to the success popups. The matching notices still go to the
    /// notice feed through the controller; this only adds the popup. Optional services are skipped when missing.
    /// </summary>
    public sealed class SuccessToastSources : MonoBehaviour
    {
        [SerializeField] SuccessToasts _toasts;
        [SerializeField] FleetPopulation _population;
        [SerializeField] CombatService _combat;
        [SerializeField] RewardDelivery _rewards;
        [SerializeField] MiningCoordinator _mining;
        [Tooltip("Optional.")]
        [SerializeField] FleetWaterSupply _water;
        [Tooltip("Optional.")]
        [SerializeField] StrandedShipRecovery _strandedShips;
        [Tooltip("Optional.")]
        [SerializeField] CrewTransferService _crewTransfer;

        void Awake()
        {
            if (_toasts == null || _population == null || _combat == null || _rewards == null || _mining == null)
            {
                Debug.LogError("SuccessToastSources needs the success toasts, the fleet population, combat, reward delivery and mining.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _population.Success += _toasts.Show;
            _combat.Success += _toasts.Show;
            _rewards.Success += _toasts.Show;
            _mining.Success += _toasts.Show;
            if (_water != null)
            {
                _water.Success += _toasts.Show;
            }
            if (_strandedShips != null)
            {
                _strandedShips.Success += _toasts.Show;
            }
            if (_crewTransfer != null)
            {
                _crewTransfer.Success += _toasts.Show;
            }
        }

        void OnDisable()
        {
            _population.Success -= _toasts.Show;
            _combat.Success -= _toasts.Show;
            _rewards.Success -= _toasts.Show;
            _mining.Success -= _toasts.Show;
            if (_water != null)
            {
                _water.Success -= _toasts.Show;
            }
            if (_strandedShips != null)
            {
                _strandedShips.Success -= _toasts.Show;
            }
            if (_crewTransfer != null)
            {
                _crewTransfer.Success -= _toasts.Show;
            }
        }
    }
}
