using LastFleet.FleetModules;
using LastFleet.Provisions;
using LastFleet.Rewards;
using UnityEngine;

namespace LastFleet.GenerationPreview
{
    /// <summary>
    /// Every run starts from scratch (designer, 2026-11-18: a roguelike, not an incremental game). The controller
    /// rebuilds the galaxy, the fleet and the quests itself; this resets the fleet-wide services that outlive a run:
    /// found items, installed fleet modules and the water state.
    /// </summary>
    public sealed class NewRunReset : MonoBehaviour
    {
        [SerializeField] GenerationPreviewController _controller;
        [SerializeField] RewardDelivery _rewards;
        [SerializeField] FleetModuleService _fleetModules;
        [Tooltip("Optional.")]
        [SerializeField] FleetModuleWindow _fleetModuleWindow;
        [Tooltip("Optional.")]
        [SerializeField] FleetWaterSupply _water;

        void Awake()
        {
            if (_controller == null || _rewards == null || _fleetModules == null)
            {
                Debug.LogError("NewRunReset needs the preview controller, the reward delivery and the fleet module service.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _controller.RunStarted += ResetRunState;
        }

        void OnDisable()
        {
            _controller.RunStarted -= ResetRunState;
        }

        void ResetRunState()
        {
            if (_fleetModuleWindow != null)
            {
                _fleetModuleWindow.Close();
            }
            _rewards.Inventory.Clear();
            _fleetModules.ResetForNewRun();
            if (_water != null)
            {
                _water.ResetForNewRun();
            }
        }
    }
}
