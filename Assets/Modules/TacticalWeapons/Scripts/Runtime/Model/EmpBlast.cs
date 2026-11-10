using System;
using LastFleet.Ships;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// How long an EMP stuns a ship: the full time at the center, falling linearly to nothing at the
    /// edge of the radius. Human ships recover faster than Pronin ones, so their full time is shorter.
    /// </summary>
    public sealed class EmpBlast
    {
        public EmpBlast(float radiusUnits, float proninStunSeconds, float humanStunSeconds)
        {
            if (radiusUnits <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusUnits), "The blast radius must be positive.");
            }
            if (proninStunSeconds < 0f || humanStunSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(proninStunSeconds), "Stun times cannot be negative.");
            }
            RadiusUnits = radiusUnits;
            ProninStunSeconds = proninStunSeconds;
            HumanStunSeconds = humanStunSeconds;
        }

        public float RadiusUnits { get; }
        public float ProninStunSeconds { get; }
        public float HumanStunSeconds { get; }

        /// <summary>Stun time for a ship of the side at the distance from the center; 0 at or beyond the edge.</summary>
        public float StunSecondsFor(Allegiance side, float distanceUnits)
        {
            float peak = side == Allegiance.Pronin ? ProninStunSeconds : HumanStunSeconds;
            float falloff = 1f - Math.Max(0f, distanceUnits) / RadiusUnits;
            return falloff > 0f ? peak * falloff : 0f;
        }
    }
}
