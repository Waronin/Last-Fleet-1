using System;
using System.Collections.Generic;
using LastFleet.Zones;

namespace LastFleet.LateSignals
{
    /// <summary>
    /// Which late signals of a system come in at a point of the countdown (GDD: late signals). Each late
    /// point of interest carries the share of the countdown after which it shows up; once received it stays
    /// known for the run, also when the fleet returns and the countdown starts again.
    /// </summary>
    public static class LateSignalSchedule
    {
        /// <summary>Receives every signal due at the given share of the countdown; collects their zones in point-of-interest order.</summary>
        public static void ReceiveDue(SystemZones system, float countdownFraction, List<ZoneRecord> received)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }
            if (received == null)
            {
                throw new ArgumentNullException(nameof(received));
            }

            received.Clear();
            foreach (ZoneRecord zone in system.Zones)
            {
                if (!zone.IsKnown && countdownFraction >= zone.PointOfInterest.LateSignalFraction && zone.ReceiveSignal())
                {
                    received.Add(zone);
                }
            }
        }
    }
}
