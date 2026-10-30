using System.Collections.Generic;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Projectile weapon look of the human fleet (GDD: human weapons make muzzle flashes and tracers):
    /// every shot leaves one of the muzzles in turn with a flash and flies as a short glowing streak
    /// towards the target at the weapon's round speed, so it arrives when the hit lands. A miss flies
    /// on past it.
    /// </summary>
    [RequireComponent(typeof(ShipWeapons))]
    public sealed class ProjectileShotView : MonoBehaviour
    {
        [Tooltip("Gun barrels; shots leave them in turn.")]
        [SerializeField] Transform[] _muzzles;
        [Tooltip("Inactive world-space line copied for every streak in flight.")]
        [SerializeField] LineRenderer _streakTemplate;
        [Tooltip("Optional. Shown at the firing muzzle for a moment.")]
        [SerializeField] Transform _muzzleFlash;
        [SerializeField, Min(0.05f)] float _streakLengthUnits = 1.5f;
        [SerializeField, Min(0.01f)] float _flashSeconds = 0.05f;
        [Tooltip("A miss passes the target this far beside it ...")]
        [SerializeField, Min(0f)] float _missOffsetUnits = 1.5f;
        [Tooltip("... and flies on this far before it fades out.")]
        [SerializeField, Min(0f)] float _missOvershootUnits = 8f;

        readonly List<Streak> _streaks = new List<Streak>();
        ShipWeapons _weapons;
        int _nextMuzzle;
        float _flashRemainingSeconds;

        sealed class Streak
        {
            public LineRenderer Line;
            public Vector3 Origin;
            public Vector3 Direction;
            public float FlightUnits;
            public float TraveledUnits;
            public bool InFlight;
        }

        void Awake()
        {
            _weapons = GetComponent<ShipWeapons>();
            if (_muzzles == null || _muzzles.Length == 0 || _streakTemplate == null || _weapons.ProjectileSpeedUnitsPerSecond <= 0f)
            {
                Debug.LogError("ProjectileShotView needs at least one muzzle, a streak template and weapons that fire rounds.", this);
                enabled = false;
                return;
            }
            _streakTemplate.gameObject.SetActive(false);
            SetFlashVisible(false);
        }

        void OnEnable()
        {
            _weapons.Fired += OnFired;
        }

        void OnDisable()
        {
            _weapons.Fired -= OnFired;
            // A ship that lands mid-burst must not leave its rounds hanging in the zone.
            foreach (Streak streak in _streaks)
            {
                Retire(streak);
            }
            _flashRemainingSeconds = 0f;
            SetFlashVisible(false);
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (Streak streak in _streaks)
            {
                if (streak.InFlight)
                {
                    Advance(streak, deltaTime);
                }
            }

            if (_flashRemainingSeconds > 0f)
            {
                _flashRemainingSeconds -= deltaTime;
                if (_flashRemainingSeconds <= 0f)
                {
                    SetFlashVisible(false);
                }
            }
        }

        void OnFired(ShipAgent target, bool hit)
        {
            Transform muzzle = _muzzles[_nextMuzzle];
            _nextMuzzle = (_nextMuzzle + 1) % _muzzles.Length;
            Vector3 origin = muzzle.position;
            Vector3 toAim = ShotAim.Point(target, hit, _missOffsetUnits) - origin;
            if (toAim.sqrMagnitude < 1e-6f)
            {
                return;
            }

            Streak streak = TakeStreak();
            streak.Origin = origin;
            streak.Direction = toAim.normalized;
            streak.FlightUnits = toAim.magnitude + (hit ? 0f : _missOvershootUnits);
            streak.TraveledUnits = 0f;
            streak.InFlight = true;
            Advance(streak, 0f);
            streak.Line.gameObject.SetActive(true);
            ShowFlash(muzzle);
        }

        void Advance(Streak streak, float deltaTime)
        {
            streak.TraveledUnits += _weapons.ProjectileSpeedUnitsPerSecond * deltaTime;
            if (streak.TraveledUnits >= streak.FlightUnits)
            {
                Retire(streak);
                return;
            }

            // The tail never starts behind the muzzle, so a fresh round grows out of the barrel.
            float head = Mathf.Min(streak.TraveledUnits + _streakLengthUnits, streak.FlightUnits);
            float tail = streak.TraveledUnits;
            streak.Line.SetPosition(0, streak.Origin + streak.Direction * tail);
            streak.Line.SetPosition(1, streak.Origin + streak.Direction * head);
        }

        /// <summary>Reuses a streak that has landed, or copies the template; bursts stay at a handful of rounds.</summary>
        Streak TakeStreak()
        {
            foreach (Streak streak in _streaks)
            {
                if (!streak.InFlight)
                {
                    return streak;
                }
            }

            LineRenderer line = Instantiate(_streakTemplate, _streakTemplate.transform.parent);
            line.name = $"{_streakTemplate.name} {_streaks.Count + 1}";
            Streak created = new Streak { Line = line };
            _streaks.Add(created);
            return created;
        }

        static void Retire(Streak streak)
        {
            streak.InFlight = false;
            streak.Line.gameObject.SetActive(false);
        }

        void ShowFlash(Transform muzzle)
        {
            if (_muzzleFlash == null)
            {
                return;
            }
            _muzzleFlash.position = muzzle.position;
            _flashRemainingSeconds = _flashSeconds;
            SetFlashVisible(true);
        }

        void SetFlashVisible(bool visible)
        {
            if (_muzzleFlash != null)
            {
                _muzzleFlash.gameObject.SetActive(visible);
            }
        }
    }
}
