using System;

namespace LastFleet.Quests
{
    /// <summary>How the radiation of a quest station builds up in docked crews (see <see cref="DockedRadiation"/>).</summary>
    public sealed class DockedRadiationSettings
    {
        public DockedRadiationSettings(float secondsToLimit, float secondsToRecover, float deathIntervalSeconds, float warningLevel)
        {
            if (warningLevel <= 0f || warningLevel >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(warningLevel), "The warning comes before the limit.");
            }
            SecondsToLimit = secondsToLimit;
            SecondsToRecover = secondsToRecover;
            DeathIntervalSeconds = deathIntervalSeconds;
            WarningLevel = warningLevel;
        }

        /// <summary>Docked time from a clean crew to the limit, where crew members start to die.</summary>
        public float SecondsToLimit { get; }

        /// <summary>Time away from a full dose back to clean.</summary>
        public float SecondsToRecover { get; }

        /// <summary>At the limit, one crew member dies per interval of further exposure.</summary>
        public float DeathIntervalSeconds { get; }

        /// <summary>Dose level (0 to 1) at which the player is warned to undock.</summary>
        public float WarningLevel { get; }

        public RadiationDose CreateDose()
        {
            return new RadiationDose(SecondsToLimit, SecondsToRecover, DeathIntervalSeconds);
        }
    }
}
