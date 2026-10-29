using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// Creates the model of a station when its zone is shown. The station models live in their own
    /// module, which depends on this one; zones only know this contract.
    /// </summary>
    public abstract class StationViewSource : ScriptableObject
    {
        /// <summary>Instantiates the model for the station's type under the zone, at its position and heading.</summary>
        public abstract Transform CreateView(ZoneStation station, Transform zone);
    }
}
