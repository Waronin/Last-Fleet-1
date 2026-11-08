using System;

namespace LastFleet.Zones
{
    /// <summary>
    /// The people living on a station during the run, and how many of them want to join the fleet
    /// (GDD: rescue). Most stay where they feel safe: about a tenth of a friendly or wary station comes
    /// along, nobody from a hostile one. On a toxic or radioactive station the few survivors all want out,
    /// and they die one by one the longer the fleet takes. Starts from the station's generated occupants.
    /// </summary>
    public sealed class StationResidents
    {
        /// <summary>Share of a safe station's people who want to come along.</summary>
        public const float WillingShareOnSafeStations = 0.1f;

        float _pendingDeaths;

        public StationResidents(StationProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            Count = profile.Occupants;
            Willing = WillingFor(profile);
        }

        /// <summary>People aboard the station right now.</summary>
        public int Count { get; private set; }

        /// <summary>People aboard who still want to join the fleet.</summary>
        public int Willing { get; private set; }

        /// <summary>People who left the station with the fleet.</summary>
        public int Rescued { get; private set; }

        /// <summary>People who died aboard, e.g. of radiation while waiting.</summary>
        public int Died { get; private set; }

        public static int WillingFor(StationProfile profile)
        {
            if (!profile.IsInhabited || profile.Attitude == StationAttitude.Hostile)
            {
                return 0;
            }
            if (profile.IsHazardous)
            {
                return profile.Occupants;
            }
            return Math.Max(1, (int)Math.Round(profile.Occupants * WillingShareOnSafeStations));
        }

        /// <summary>Takes up to the given number of willing people off the station; returns how many came.</summary>
        public int Board(int people)
        {
            if (people < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(people), "Cannot board a negative number of people.");
            }
            int boarding = Math.Min(people, Willing);
            Willing -= boarding;
            Count -= boarding;
            Rescued += boarding;
            return boarding;
        }

        /// <summary>
        /// People die at the given share of those aboard per minute, the willing among them first, since on a
        /// hazardous station everyone is willing. Returns how many died in this step.
        /// </summary>
        public int Perish(float shareOfCountPerMinute, float deltaSeconds)
        {
            if (shareOfCountPerMinute < 0f || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Rate and time must not be negative.");
            }
            if (Count == 0)
            {
                _pendingDeaths = 0f;
                return 0;
            }

            _pendingDeaths += Count * shareOfCountPerMinute / 60f * deltaSeconds;
            int deaths = Math.Min(Count, (int)_pendingDeaths);
            _pendingDeaths -= deaths;
            Count -= deaths;
            Willing = Math.Max(0, Willing - deaths);
            Died += deaths;
            return deaths;
        }
    }
}
