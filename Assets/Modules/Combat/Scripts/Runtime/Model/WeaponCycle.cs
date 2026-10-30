using System;

namespace LastFleet.Combat
{
    public enum ShotResult
    {
        /// <summary>Reloading, or the target is out of range: nothing was fired.</summary>
        NotFired,
        Miss,
        Hit
    }

    /// <summary>
    /// Rate of fire and hit rolls of one weapon, without Unity lifecycle so the rules stay testable.
    /// Shots come in bursts with a pause after each; a shot is only taken at a target in range, and
    /// missing still costs the time to the next shot.
    /// </summary>
    public sealed class WeaponCycle
    {
        readonly WeaponSettings _settings;
        int _roundsFiredInBurst;

        public WeaponCycle(WeaponSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public float ReloadRemainingSeconds { get; private set; }
        public bool IsReady => ReloadRemainingSeconds <= 0f;

        public void Tick(float deltaTime)
        {
            ReloadRemainingSeconds = Math.Max(0f, ReloadRemainingSeconds - deltaTime);
        }

        /// <summary>Fires at a target this far away if the weapon is ready; the roll from 0 to 1 decides a hit.</summary>
        public ShotResult TryFire(float distanceUnits, float roll)
        {
            if (!IsReady || distanceUnits > _settings.RangeUnits)
            {
                return ShotResult.NotFired;
            }

            _roundsFiredInBurst++;
            if (_roundsFiredInBurst >= _settings.RoundsPerBurst)
            {
                _roundsFiredInBurst = 0;
                ReloadRemainingSeconds = Math.Max(_settings.SecondsBetweenShots, _settings.SecondsBetweenBursts);
            }
            else
            {
                ReloadRemainingSeconds = _settings.SecondsBetweenShots;
            }
            return roll < _settings.HitChance(distanceUnits) ? ShotResult.Hit : ShotResult.Miss;
        }
    }
}
