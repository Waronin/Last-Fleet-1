using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Energy weapon look (GDD: Pronin energy weapons draw clean, bright beams): a straight line from
    /// the emitter to the target that flashes for a moment with every shot.
    /// </summary>
    [RequireComponent(typeof(ShipWeapons))]
    public sealed class BeamShotView : MonoBehaviour
    {
        [Tooltip("World-space line from the emitter (its own position) to the target.")]
        [SerializeField] LineRenderer _beam;
        [SerializeField, Min(0.01f)] float _beamSeconds = 0.08f;
        [Tooltip("A miss ends the beam this far beside the target.")]
        [SerializeField, Min(0f)] float _missOffsetUnits = 1.5f;

        ShipWeapons _weapons;
        float _remainingSeconds;

        void Awake()
        {
            _weapons = GetComponent<ShipWeapons>();
            if (_beam == null)
            {
                Debug.LogError("BeamShotView needs a beam line.", this);
                enabled = false;
                return;
            }
            _beam.enabled = false;
        }

        void OnEnable()
        {
            _weapons.Fired += OnFired;
        }

        void OnDisable()
        {
            _weapons.Fired -= OnFired;
            if (_beam == null)
            {
                return;
            }
            // A ship that lands mid-shot must not leave its beam hanging in the zone.
            _beam.enabled = false;
            _remainingSeconds = 0f;
        }

        void Update()
        {
            if (_remainingSeconds <= 0f)
            {
                return;
            }
            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds <= 0f)
            {
                _beam.enabled = false;
            }
        }

        void OnFired(ShipAgent target, bool hit)
        {
            _beam.SetPosition(0, _beam.transform.position);
            _beam.SetPosition(1, ShotAim.Point(target, hit, _missOffsetUnits));
            _beam.enabled = true;
            _remainingSeconds = _beamSeconds;
        }
    }
}
