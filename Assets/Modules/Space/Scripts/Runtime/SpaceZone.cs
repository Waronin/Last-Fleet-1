using UnityEngine;

namespace LastFleet.Space
{
    /// <summary>
    /// A local zone inside a star system (GDD: Zones). The zone is a circular plane on which ships
    /// are placed and moved. Its radius is configured in klicks.
    /// </summary>
    [ExecuteAlways]
    public sealed class SpaceZone : MonoBehaviour
    {
        const int GizmoSegmentCount = 96;
        const float GridOversize = 1.025f;
        static readonly int ZoneRadiusId = Shader.PropertyToID("_ZoneRadius");

        [SerializeField] string _displayName = "Zone";
        [SerializeField, Min(1f)] float _radiusKlicks = 50f;
        [Tooltip("Flat quad using the ZoneGrid material. It is scaled to the zone diameter.")]
        [SerializeField] Renderer _gridRenderer;

        MaterialPropertyBlock _propertyBlock;

        public string DisplayName => _displayName;
        public float RadiusKlicks => _radiusKlicks;
        public float RadiusUnits => SpaceUnits.KlicksToUnits(_radiusKlicks);
        public Vector3 Center => transform.position;
        public Plane PlacementPlane => new Plane(transform.up, transform.position);

        public bool GridVisible
        {
            get => _gridRenderer != null && _gridRenderer.enabled;
            set
            {
                if (_gridRenderer != null)
                {
                    _gridRenderer.enabled = value;
                }
            }
        }

        /// <summary>Configures a zone created at runtime.</summary>
        public void Initialize(string displayName, float radiusKlicks, Renderer gridRenderer)
        {
            _displayName = displayName;
            _radiusKlicks = Mathf.Max(1f, radiusKlicks);
            _gridRenderer = gridRenderer;
            ApplyGrid();
        }

        void OnEnable()
        {
            ApplyGrid();
        }

        void OnValidate()
        {
            ApplyGrid();
        }

        /// <summary>Intersects a ray with the zone plane, e.g. for mouse placement.</summary>
        public bool TryRaycastPlane(Ray ray, out Vector3 point)
        {
            if (PlacementPlane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            point = default;
            return false;
        }

        /// <summary>Projects a point onto the zone plane and limits it to the zone radius.</summary>
        public Vector3 ClampToZone(Vector3 worldPoint)
        {
            Vector3 offset = Vector3.ProjectOnPlane(worldPoint - Center, transform.up);
            return Center + Vector3.ClampMagnitude(offset, RadiusUnits);
        }

        public bool Contains(Vector3 worldPoint)
        {
            Vector3 offset = Vector3.ProjectOnPlane(worldPoint - Center, transform.up);
            return offset.sqrMagnitude <= RadiusUnits * RadiusUnits;
        }

        void ApplyGrid()
        {
            if (_gridRenderer == null)
            {
                return;
            }

            float diameter = RadiusUnits * 2f * GridOversize;
            _gridRenderer.transform.localScale = new Vector3(diameter, diameter, 1f);

            _propertyBlock ??= new MaterialPropertyBlock();
            _gridRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(ZoneRadiusId, RadiusUnits);
            _gridRenderer.SetPropertyBlock(_propertyBlock);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.6f);
            Vector3 right = transform.right * RadiusUnits;
            Vector3 forward = transform.forward * RadiusUnits;
            Vector3 previous = Center + right;
            for (int i = 1; i <= GizmoSegmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / GizmoSegmentCount;
                Vector3 next = Center + right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
