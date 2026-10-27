using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Space
{
    /// <summary>
    /// Sky and lighting of a space scene: skybox, optional sun and ambient light. Passes the sun
    /// direction to the starfield shader so the visible sun always matches the light direction.
    /// </summary>
    [ExecuteAlways]
    public sealed class SpaceEnvironment : MonoBehaviour
    {
        static readonly int SunDirectionId = Shader.PropertyToID("_LF_SunDirection");

        [Tooltip("Optional. Without a sun the skybox shows no sun disc.")]
        [SerializeField] Light _sun;
        [SerializeField] Material _skybox;

        [Header("Ambient Light")]
        [Tooltip("Faint ambient light keeps the dark sides of ships readable.")]
        [SerializeField] Color _ambientSky = new Color(0.045f, 0.055f, 0.08f);
        [SerializeField] Color _ambientEquator = new Color(0.03f, 0.03f, 0.045f);
        [SerializeField] Color _ambientGround = new Color(0.015f, 0.015f, 0.02f);

        public Light Sun => _sun;

        /// <summary>Changes which light is shown as the sun disc in the sky; null hides the sun.</summary>
        public void SetSun(Light sun)
        {
            _sun = sun;
            RenderSettings.sun = sun;
            UpdateSunDirection();
        }

        void OnEnable()
        {
            if (_skybox == null)
            {
                Debug.LogError("SpaceEnvironment needs a skybox material.", this);
            }
            Apply();
        }

        void OnValidate()
        {
            Apply();
        }

        void LateUpdate()
        {
            UpdateSunDirection();
        }

        void Apply()
        {
            if (_skybox != null)
            {
                RenderSettings.skybox = _skybox;
            }

            RenderSettings.sun = _sun;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _ambientSky;
            RenderSettings.ambientEquatorColor = _ambientEquator;
            RenderSettings.ambientGroundColor = _ambientGround;
            UpdateSunDirection();
        }

        void UpdateSunDirection()
        {
            Vector3 towardsSun = _sun != null ? -_sun.transform.forward : Vector3.zero;
            Shader.SetGlobalVector(SunDirectionId, towardsSun);
        }
    }
}
