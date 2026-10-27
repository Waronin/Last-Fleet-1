using System;
using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using UnityEngine;

namespace LastFleet.Galaxy
{
    /// <summary>
    /// Blockout star map: one sphere per system, the jump range around the selected system and
    /// lines to every system reachable with one jump. Holds no game state besides the selection.
    /// </summary>
    public sealed class GalaxyMapView : MonoBehaviour
    {
        const int RingSegmentCount = 96;

        [SerializeField] Material _markerMaterial;
        [SerializeField] Material _lineMaterial;
        [SerializeField, Min(0.01f)] float _unitsPerLightYear = 1f;
        [SerializeField, Min(0.1f)] float _markerDiameterUnits = 5f;
        [SerializeField, Min(0.01f)] float _lineWidthUnits = 1.2f;

        [Header("Colors")]
        [SerializeField] Color _systemColor = new Color(0.7f, 0.75f, 0.85f);
        [SerializeField] Color _startSystemColor = new Color(0.3f, 1f, 0.45f);
        [SerializeField] Color _selectedSystemColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] Color _reachableSystemColor = new Color(0.4f, 0.75f, 1f);
        [SerializeField] Color _jumpLineColor = new Color(0.3f, 0.6f, 1f);

        readonly List<Renderer> _markers = new List<Renderer>();
        readonly List<LineRenderer> _jumpLines = new List<LineRenderer>();
        LineRenderer _jumpRangeRing;
        MaterialPropertyBlock _propertyBlock;
        GalaxyLayout _galaxy;

        public GalaxyLayout Galaxy => _galaxy;
        public int SelectedSystemIndex { get; private set; } = -1;
        public float ExtentUnits { get; private set; }

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (_markerMaterial == null || _lineMaterial == null)
            {
                Debug.LogError("GalaxyMapView needs a marker and a line material.", this);
            }
        }

        public void Show(GalaxyLayout galaxy)
        {
            if (galaxy == null)
            {
                throw new ArgumentNullException(nameof(galaxy));
            }

            Clear();
            _galaxy = galaxy;

            float extent = 0f;
            foreach (StarSystemNode system in galaxy.Systems)
            {
                Renderer marker = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, transform, _markerMaterial, system.Name);
                marker.transform.localPosition = system.PositionLightYears * _unitsPerLightYear;
                marker.transform.localScale = Vector3.one * _markerDiameterUnits;
                _markers.Add(marker);
                extent = Mathf.Max(extent, marker.transform.localPosition.magnitude);
            }
            ExtentUnits = extent + _markerDiameterUnits;

            _jumpRangeRing = BlockoutPrimitives.CreateLine(transform, _lineMaterial, "Jump Range", _lineWidthUnits);
            BlockoutPrimitives.SetColor(_jumpRangeRing, _jumpLineColor, _propertyBlock);
            Select(galaxy.StartSystemIndex);
        }

        public void Select(int systemIndex)
        {
            if (_galaxy == null)
            {
                throw new InvalidOperationException("Show a galaxy before selecting a system.");
            }
            if (systemIndex < 0 || systemIndex >= _galaxy.Systems.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(systemIndex));
            }

            SelectedSystemIndex = systemIndex;
            RefreshMarkerColors();
            RefreshJumpLines();
        }

        public Vector3 GetSystemWorldPosition(int systemIndex)
        {
            return _markers[systemIndex].transform.position;
        }

        /// <summary>
        /// Finds the system closest to a screen position. Picking in screen space keeps small markers
        /// clickable at every zoom level (GDD: no one should have to hit tiny objects with the mouse).
        /// </summary>
        public bool TryPickSystem(Camera camera, Vector2 screenPosition, float maximumDistancePixels, out int systemIndex)
        {
            systemIndex = -1;
            float bestDistanceSquared = maximumDistancePixels * maximumDistancePixels;
            for (int i = 0; i < _markers.Count; i++)
            {
                Vector3 screenPoint = camera.WorldToScreenPoint(_markers[i].transform.position);
                if (screenPoint.z <= 0f)
                {
                    continue;
                }

                float distanceSquared = (new Vector2(screenPoint.x, screenPoint.y) - screenPosition).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    systemIndex = i;
                }
            }
            return systemIndex >= 0;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Clear()
        {
            BlockoutPrimitives.DestroyChildren(transform);
            _markers.Clear();
            _jumpLines.Clear();
            _jumpRangeRing = null;
            _galaxy = null;
            SelectedSystemIndex = -1;
            ExtentUnits = 0f;
        }

        void RefreshMarkerColors()
        {
            IReadOnlyList<int> reachable = _galaxy.GetNeighbours(SelectedSystemIndex);
            for (int i = 0; i < _markers.Count; i++)
            {
                Color color = _systemColor;
                if (i == _galaxy.StartSystemIndex)
                {
                    color = _startSystemColor;
                }
                if (Contains(reachable, i))
                {
                    color = _reachableSystemColor;
                }
                if (i == SelectedSystemIndex)
                {
                    color = _selectedSystemColor;
                }
                BlockoutPrimitives.SetColor(_markers[i], color, _propertyBlock);
            }
        }

        void RefreshJumpLines()
        {
            Vector3 origin = _markers[SelectedSystemIndex].transform.localPosition;
            BlockoutPrimitives.SetCircle(_jumpRangeRing, origin, _galaxy.JumpRangeLightYears * _unitsPerLightYear, RingSegmentCount);

            IReadOnlyList<int> reachable = _galaxy.GetNeighbours(SelectedSystemIndex);
            while (_jumpLines.Count < reachable.Count)
            {
                LineRenderer line = BlockoutPrimitives.CreateLine(transform, _lineMaterial, "Jump Line", _lineWidthUnits);
                BlockoutPrimitives.SetColor(line, _jumpLineColor, _propertyBlock);
                line.positionCount = 2;
                _jumpLines.Add(line);
            }

            for (int i = 0; i < _jumpLines.Count; i++)
            {
                bool used = i < reachable.Count;
                _jumpLines[i].gameObject.SetActive(used);
                if (used)
                {
                    _jumpLines[i].SetPosition(0, origin);
                    _jumpLines[i].SetPosition(1, _markers[reachable[i]].transform.localPosition);
                }
            }
        }

        static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == value)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
