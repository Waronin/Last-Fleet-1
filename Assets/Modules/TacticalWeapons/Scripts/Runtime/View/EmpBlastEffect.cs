using UnityEngine;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// Blockout EMP blast: a ring races out to the edge of the blast radius and then thins out, so
    /// the player sees which ships it reached. It removes itself when done.
    /// </summary>
    public sealed class EmpBlastEffect : MonoBehaviour
    {
        [Tooltip("Unit circle on the zone plane, scaled to the blast radius.")]
        [SerializeField] LineRenderer _ring;
        [SerializeField, Min(0.05f)] float _expandSeconds = 0.5f;
        [SerializeField, Min(0.05f)] float _fadeSeconds = 1.5f;
        [SerializeField, Min(0.01f)] float _ringWidthUnits = 2f;

        float _radiusUnits = 1f;
        float _elapsedSeconds;

        public void Play(float radiusUnits)
        {
            _radiusUnits = radiusUnits;
            _elapsedSeconds = 0f;
            Step(0f);
        }

        void Update()
        {
            Step(Time.deltaTime);
        }

        void Step(float deltaTime)
        {
            _elapsedSeconds += deltaTime;
            if (_elapsedSeconds >= _expandSeconds + _fadeSeconds)
            {
                Destroy(gameObject);
                return;
            }
            if (_ring == null)
            {
                return;
            }

            float expand = Mathf.Clamp01(_elapsedSeconds / _expandSeconds);
            float fade = Mathf.Clamp01((_elapsedSeconds - _expandSeconds) / _fadeSeconds);
            // The ring is a unit circle: scaling it keeps the line width, so the width is set apart.
            _ring.transform.localScale = Vector3.one * (_radiusUnits * (1f - (1f - expand) * (1f - expand)));
            _ring.widthMultiplier = _ringWidthUnits * (1f - fade);
        }
    }
}
