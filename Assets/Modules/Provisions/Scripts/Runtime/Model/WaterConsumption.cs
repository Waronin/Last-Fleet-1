using System;

namespace LastFleet.Provisions
{
    /// <summary>What one step of game time means for the fleet's water.</summary>
    public readonly struct WaterStep
    {
        public WaterStep(float waterToTake, int deaths, bool becameLow, bool ranDry)
        {
            WaterToTake = waterToTake;
            Deaths = deaths;
            BecameLow = becameLow;
            RanDry = ranDry;
        }

        /// <summary>Water drunk in this step; never more than there is.</summary>
        public float WaterToTake { get; }

        /// <summary>People who died of thirst and are to be removed now; collected into batches.</summary>
        public int Deaths { get; }

        /// <summary>The water just dropped below the warning time.</summary>
        public bool BecameLow { get; }

        /// <summary>The water just ran out.</summary>
        public bool RanDry { get; }
    }

    /// <summary>
    /// The fleet's water demand (GDD: water for the people; food is left implicit): every person drinks,
    /// so demand grows with the population. Without water, a share of the people dies per minute, in
    /// proportion to the shortage. Deaths are released in batches so the player gets a few clear reports
    /// instead of one per person. Plain time in, so time compression only feeds it more seconds.
    /// </summary>
    public sealed class WaterConsumption
    {
        readonly float _peoplePerUnitPerMinute;
        readonly float _deathFractionPerMinute;
        readonly float _warningMinutes;
        readonly float _deathBatchSeconds;
        float _pendingDeaths;
        float _secondsSinceBatch;

        public WaterConsumption(float peoplePerUnitPerMinute, float deathFractionPerMinute, float warningMinutes, float deathBatchSeconds)
        {
            if (peoplePerUnitPerMinute <= 0f || deathFractionPerMinute <= 0f || warningMinutes < 0f || deathBatchSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(peoplePerUnitPerMinute), "Water rules need positive rates and batch time.");
            }
            _peoplePerUnitPerMinute = peoplePerUnitPerMinute;
            _deathFractionPerMinute = deathFractionPerMinute;
            _warningMinutes = warningMinutes;
            _deathBatchSeconds = deathBatchSeconds;
        }

        public bool IsLow { get; private set; }
        public bool IsDry { get; private set; }

        public float DemandPerSecond(int people)
        {
            return Math.Max(0, people) / _peoplePerUnitPerMinute / 60f;
        }

        /// <summary>How long the water lasts at the current population; infinity without people.</summary>
        public float MinutesLeft(float water, int people)
        {
            float demandPerMinute = DemandPerSecond(people) * 60f;
            return demandPerMinute > 0f ? Math.Max(0f, water) / demandPerMinute : float.PositiveInfinity;
        }

        public WaterStep Advance(float deltaSeconds, int people, float waterAvailable)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }

            float demand = DemandPerSecond(people) * deltaSeconds;
            float taken = Math.Min(demand, Math.Max(0f, waterAvailable));
            float shortage = demand > 0f ? 1f - taken / demand : 0f;
            _pendingDeaths += people * _deathFractionPerMinute / 60f * deltaSeconds * shortage;

            bool wasLow = IsLow;
            bool wasDry = IsDry;
            float left = Math.Max(0f, waterAvailable) - taken;
            IsDry = people > 0 && left <= 0f;
            IsLow = people > 0 && MinutesLeft(left, people) <= _warningMinutes;

            int deaths = 0;
            _secondsSinceBatch += deltaSeconds;
            if (_secondsSinceBatch >= _deathBatchSeconds)
            {
                _secondsSinceBatch = 0f;
                // A hair of tolerance, so float rounding does not hold back a whole person.
                deaths = Math.Min(people, (int)(_pendingDeaths + 1e-3f));
                _pendingDeaths = Math.Max(0f, _pendingDeaths - deaths);
            }
            if (!IsDry && shortage <= 0f && _pendingDeaths < 1f)
            {
                // Less than one person short is forgiven once there is water again.
                _pendingDeaths = 0f;
            }
            return new WaterStep(taken, deaths, IsLow && !wasLow, IsDry && !wasDry);
        }
    }
}
