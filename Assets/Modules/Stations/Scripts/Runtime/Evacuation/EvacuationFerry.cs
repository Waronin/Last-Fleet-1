using System;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>
    /// Marks a ship as busy while it ferries people for the <see cref="EvacuationService"/>, so automatic
    /// tasks such as scans do not pull it away between trips. Added by the service on first use. After
    /// an interruption such as refueling, the ferry takes up the same evacuation again.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class EvacuationFerry : MonoBehaviour, IShipAutomation, IResumableAutomation
    {
        EvacuationService _service;
        ShipAgent _ship;

        public bool IsAutomationActive => _service != null && _service.IsFerrying(_ship);

        public void Initialize(EvacuationService service)
        {
            _service = service;
            _ship = GetComponent<ShipAgent>();
        }

        public Action Suspend()
        {
            return _service != null ? _service.Suspend(_ship) : null;
        }
    }
}
