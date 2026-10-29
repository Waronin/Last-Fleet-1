using System.Collections.Generic;
using LastFleet.Zones;
using UnityEngine;
using UnityEngine.Serialization;

namespace LastFleet.Stations
{
    /// <summary>
    /// The model of one station type at its real size, with its docking ports. Each port sits on the
    /// outer face of its frame, and its forward axis points away from the station: a ship docks against
    /// it with its own airlock.
    /// </summary>
    public sealed class StationView : MonoBehaviour
    {
        [SerializeField] StationType _type;
        [FormerlySerializedAs("_dockingBays")]
        [SerializeField] Transform[] _dockingPorts = new Transform[0];

        public StationType Type => _type;
        public IReadOnlyList<Transform> DockingPorts => _dockingPorts;
    }
}
