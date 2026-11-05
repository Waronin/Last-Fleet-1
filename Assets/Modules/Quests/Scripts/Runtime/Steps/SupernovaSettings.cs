using System;
using LastFleet.Dialogue;

namespace LastFleet.Quests
{
    /// <summary>What a <see cref="SupernovaStep"/> is about: holding the generator station, then the time to get away.</summary>
    public sealed class SupernovaSettings
    {
        public SupernovaSettings(StationProcessingSettings hold, float countdownSeconds, Func<DialogueScript> ignited, Func<string, DialogueScript> escaped)
        {
            if (countdownSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(countdownSeconds), "The countdown needs a positive time.");
            }
            Hold = hold ?? throw new ArgumentNullException(nameof(hold));
            if (hold.Output != null)
            {
                throw new ArgumentException("The generator consumes its fuel; the hold has no result.", nameof(hold));
            }
            CountdownSeconds = countdownSeconds;
            Ignited = ignited;
            Escaped = escaped;
        }

        /// <summary>Hand the fuel over and keep the station manned until the generator ignites.</summary>
        public StationProcessingSettings Hold { get; }

        /// <summary>Time from ignition to the supernova; the fleet has to jump out before it ends.</summary>
        public float CountdownSeconds { get; }

        /// <summary>Optional radio call at ignition.</summary>
        public Func<DialogueScript> Ignited { get; }

        /// <summary>Optional radio call once the fleet got away; takes the name of the system left behind.</summary>
        public Func<string, DialogueScript> Escaped { get; }
    }
}
