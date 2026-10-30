using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Blockout explosion of a destroyed ship: a bright fireball that swells and collapses, a shock
    /// ring that races outwards and a spray of debris. It removes itself when done.
    /// </summary>
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] Transform _fireball;
        [Tooltip("Unit circle on the zone plane, scaled outwards.")]
        [SerializeField] LineRenderer _shockRing;
        [SerializeField] ParticleSystem _debris;
        [SerializeField, Min(0.1f)] float _fireballSeconds = 0.6f;
        [SerializeField, Min(0.1f)] float _shockRingSeconds = 0.9f;
        [SerializeField, Min(0.1f)] float _lifetimeSeconds = 2.5f;
        [SerializeField, Min(1)] int _debrisCount = 24;

        float _size = 1f;
        float _elapsedSeconds;

        /// <summary>Starts the explosion; the size is roughly the ship's length in units.</summary>
        public void Play(float size)
        {
            _size = size;
            _elapsedSeconds = 0f;
            if (_debris != null)
            {
                _debris.transform.localScale = Vector3.one * size;
                _debris.Emit(_debrisCount);
            }
            Step(0f);
        }

        void Update()
        {
            Step(Time.deltaTime);
        }

        void Step(float deltaTime)
        {
            _elapsedSeconds += deltaTime;
            if (_elapsedSeconds >= _lifetimeSeconds)
            {
                Destroy(gameObject);
                return;
            }

            if (_fireball != null)
            {
                // Swells fast, then burns out.
                float t = _elapsedSeconds / _fireballSeconds;
                float scale = t < 0.25f ? t / 0.25f : Mathf.Max(0f, 1f - (t - 0.25f) / 0.75f);
                _fireball.localScale = Vector3.one * (_size * 1.5f * scale);
                _fireball.gameObject.SetActive(scale > 0f);
            }
            if (_shockRing != null)
            {
                float t = _elapsedSeconds / _shockRingSeconds;
                _shockRing.enabled = t < 1f;
                _shockRing.transform.localScale = Vector3.one * (_size * 4f * t);
            }
        }
    }
}
