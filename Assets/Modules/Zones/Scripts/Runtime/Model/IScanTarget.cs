using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// Something in a zone that a ship can scan on site, such as a deposit or a station (GDD: what a
    /// zone really holds is only known when a ship is there). The ship flies to the standoff distance,
    /// scans for its base time multiplied by the target's factor, then completes the scan.
    /// </summary>
    public interface IScanTarget
    {
        Vector3 LocalPosition { get; }

        /// <summary>Distance from the center that the scanning ship keeps, outside the target.</summary>
        float ScanStandoffUnits { get; }

        /// <summary>Multiplies the scanning ship's base duration; larger targets take longer.</summary>
        float ScanDurationFactor { get; }

        bool IsScanned { get; }

        /// <summary>Records what the scan found; the roll from 0 to 1 decides how far an estimate is off.</summary>
        void CompleteScan(float accuracyRoll);
    }
}
