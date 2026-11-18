using System;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// GDD: the player loses when all capital ships are destroyed. Watches the fleet's losses and reports the defeat a
    /// moment after the last capital ship falls, so its end can still be seen. Ships that leave with a run or a star
    /// system are removed without being lost and do not count.
    /// </summary>
    public sealed class CapitalShipLossWatch : MonoBehaviour
    {
        [SerializeField] ShipRegistry _fleet;
        [Tooltip("Game seconds between the loss of the last capital ship and the defeat.")]
        [SerializeField, Min(0f)] float _delaySeconds = 2.5f;

        float _secondsUntilDefeat = -1f;

        /// <summary>The fleet has no capital ship left.</summary>
        public event Action FleetDestroyed;

        public bool IsDefeatPending => _secondsUntilDefeat >= 0f;

        void Awake()
        {
            if (_fleet == null)
            {
                Debug.LogError("CapitalShipLossWatch needs the fleet's ship registry.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _fleet.ShipLost += OnShipLost;
        }

        void OnDisable()
        {
            _fleet.ShipLost -= OnShipLost;
        }

        /// <summary>Forgets a pending defeat, e.g. when a new run starts.</summary>
        public void ResetWatch()
        {
            _secondsUntilDefeat = -1f;
        }

        void Update()
        {
            if (!IsDefeatPending)
            {
                return;
            }
            _secondsUntilDefeat -= Time.deltaTime;
            if (_secondsUntilDefeat < 0f)
            {
                _secondsUntilDefeat = -1f;
                FleetDestroyed?.Invoke();
            }
        }

        void OnShipLost(ShipAgent ship, Shared.Text.GameText cause)
        {
            if (!ship.IsCapital || IsDefeatPending || HasCapitalShipLeft(ship))
            {
                return;
            }
            _secondsUntilDefeat = _delaySeconds;
        }

        /// <summary>Ships destroyed this frame stay registered until the end of the frame, so they do not count.</summary>
        bool HasCapitalShipLeft(ShipAgent lost)
        {
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (ship == lost || !ship.IsCapital)
                {
                    continue;
                }
                if (ship.TryGetComponent(out ShipDurability durability) && durability.Hull.IsDestroyed)
                {
                    continue;
                }
                return true;
            }
            return false;
        }
    }
}
