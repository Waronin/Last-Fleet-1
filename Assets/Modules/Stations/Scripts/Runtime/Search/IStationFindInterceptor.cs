using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Stations
{
    /// <summary>
    /// Takes over a find before it is handed out, e.g. a quest whose trap goes off instead of the
    /// first find. Registered with <see cref="SearchService.AddInterceptor"/>.
    /// </summary>
    public interface IStationFindInterceptor
    {
        /// <summary>
        /// Returns true to take the find: nothing is handed out and every search at the station stops.
        /// </summary>
        bool TryIntercept(ZoneStation station, ShipAgent finder);
    }
}
