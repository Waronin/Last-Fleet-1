using System.Collections.Generic;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>
    /// The model of one station type at its real size, with its docking bays. Each bay's forward
    /// axis points out of the bay, so a ship approaches against it, like a hangar's landing bay.
    /// </summary>
    public sealed class StationView : MonoBehaviour
    {
        [SerializeField] StationType _type;
        [SerializeField] Transform[] _dockingBays = new Transform[0];

        public StationType Type => _type;
        public IReadOnlyList<Transform> DockingBays => _dockingBays;
    }
}
