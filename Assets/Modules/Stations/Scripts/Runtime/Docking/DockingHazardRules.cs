using System;
using System.Collections.Generic;
using LastFleet.Zones;

namespace LastFleet.Stations
{
    public enum DockingHazard
    {
        /// <summary>A crew member dies in the toxic atmosphere.</summary>
        ToxicAtmosphere,

        /// <summary>A crew member dies of radiation.</summary>
        Radiation,

        /// <summary>The hostile station crew kills a crew member.</summary>
        HostileAttack,

        /// <summary>The hostile station crew captures the ship; everyone aboard is lost.</summary>
        Capture
    }

    /// <summary>
    /// What threatens a ship while it is docked at a station (GDD: a hostile environment harms the
    /// crew; a hostile crew harms them and can capture the docked ship). Every hazard has an
    /// exponential waiting time; values are balancing starting points. Thin air, friendly and wary
    /// crews are harmless.
    /// </summary>
    public static class DockingHazardRules
    {
        public const float ToxicAtmosphereMeanSeconds = 30f;
        public const float RadiationMeanSeconds = 30f;
        public const float HostileAttackMeanSeconds = 20f;
        public const float CaptureMeanSeconds = 60f;

        /// <summary>The hazards of a station, in the order they are rolled.</summary>
        public static void CollectHazards(StationProfile profile, List<DockingHazard> hazards)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            hazards.Clear();
            if (profile.Atmosphere == StationAtmosphere.Toxic)
            {
                hazards.Add(DockingHazard.ToxicAtmosphere);
            }
            if (profile.Radiation == StationRadiation.Elevated)
            {
                hazards.Add(DockingHazard.Radiation);
            }
            if (profile.Attitude == StationAttitude.Hostile)
            {
                hazards.Add(DockingHazard.HostileAttack);
                hazards.Add(DockingHazard.Capture);
            }
        }

        public static float MeanSeconds(DockingHazard hazard)
        {
            return hazard switch
            {
                DockingHazard.ToxicAtmosphere => ToxicAtmosphereMeanSeconds,
                DockingHazard.Radiation => RadiationMeanSeconds,
                DockingHazard.HostileAttack => HostileAttackMeanSeconds,
                DockingHazard.Capture => CaptureMeanSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(hazard), hazard, null)
            };
        }
    }
}
