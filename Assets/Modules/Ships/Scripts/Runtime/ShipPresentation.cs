using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Blockout visuals of a ship: engine glow that follows thrust, an engine trail and a bracket of
    /// constant screen size so small ships stay visible and clickable (GDD: Brackets, Light LOD).
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class ShipPresentation : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform _engineGlow;
        [SerializeField] TrailRenderer _trail;
        [SerializeField] LineRenderer _bracket;
        [Tooltip("Optional. The bracket follows this visual root, e.g. when mining moves the model onto a rock.")]
        [SerializeField] Transform _model;
        [SerializeField, Min(1f)] float _bracketSizePixels = 22f;
        [SerializeField, Min(0.01f)] float _bracketLineFraction = 0.08f;
        [SerializeField, Min(0f)] float _engineGlowMaximumScale = 1f;
        [SerializeField, Range(0f, 1f)] float _thrusterGlowFraction = 0.35f;
        [SerializeField] Color _bracketColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] Color _selectedBracketColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("GDD: enemies are red in every zoom level.")]
        [SerializeField] Color _hostileBracketColor = new Color(1f, 0.25f, 0.2f);

        ShipAgent _ship;
        Camera _camera;
        MaterialPropertyBlock _propertyBlock;

        public bool IsSelected { get; private set; }

        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _propertyBlock = new MaterialPropertyBlock();
            if (_engineGlow == null || _trail == null || _bracket == null)
            {
                Debug.LogError("ShipPresentation needs an engine glow, a trail and a bracket.", this);
                enabled = false;
                return;
            }
            RefreshBracket();
        }

        void OnEnable()
        {
            _ship.ZoneChanged += OnZoneChanged;
            // A ship leaving a hangar must not draw a trail from where it landed.
            _trail.Clear();
        }

        void OnDisable()
        {
            _ship.ZoneChanged -= OnZoneChanged;
        }

        public void Initialize(Camera camera)
        {
            _camera = camera;
        }

        /// <summary>Stops the engine trail, e.g. while the model is moved without the ship flying.</summary>
        public void SetTrailEmitting(bool emitting)
        {
            _trail.emitting = emitting;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            RefreshBracket();
        }

        /// <summary>Colors the bracket for the selection and the ship's side, e.g. after it changed sides.</summary>
        public void RefreshBracket()
        {
            if (_propertyBlock == null || _bracket == null)
            {
                return;
            }
            if (IsSelected)
            {
                SetBracketColor(_selectedBracketColor);
                return;
            }
            SetBracketColor(_ship.Allegiance == Allegiance.HumanFleet ? _bracketColor : _hostileBracketColor);
        }

        void LateUpdate()
        {
            float thrust = _ship.MainDriveActive ? 1f : _ship.ThrustersActive ? _thrusterGlowFraction : 0f;
            _engineGlow.localScale = Vector3.one * (_engineGlowMaximumScale * thrust);

            if (_camera == null)
            {
                return;
            }

            // Size in world units that covers the configured number of pixels at the ship's distance.
            float distance = Vector3.Distance(_camera.transform.position, transform.position);
            float worldPerPixel = 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            float size = worldPerPixel * _bracketSizePixels;
            Transform bracketTransform = _bracket.transform;
            if (_model != null)
            {
                bracketTransform.position = _model.position;
            }
            bracketTransform.rotation = _camera.transform.rotation;
            bracketTransform.localScale = Vector3.one * size;
            _bracket.widthMultiplier = size * _bracketLineFraction;
        }

        void OnZoneChanged(ShipAgent ship)
        {
            // Without clearing, the trail would draw a line across the gap between the zones.
            _trail.Clear();
        }

        void SetBracketColor(Color color)
        {
            _bracket.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _bracket.SetPropertyBlock(_propertyBlock);
        }
    }
}
