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
        [Tooltip("Only for the stranded ship type: which capital ship class this hulk shows.")]
        [SerializeField] StrandedShipClass _strandedClass;
        [FormerlySerializedAs("_dockingBays")]
        [SerializeField] Transform[] _dockingPorts = new Transform[0];

        public StationType Type => _type;
        public StrandedShipClass StrandedClass => _strandedClass;

        /// <summary>Whether this model is the one for the station: same type, and for a stranded ship the same class.</summary>
        public bool Shows(ZoneStation station)
        {
            return station.Type == _type && (!station.IsStrandedShip || station.StrandedHull.Class == _strandedClass);
        }
        public IReadOnlyList<Transform> DockingPorts => _dockingPorts;
    }
}
