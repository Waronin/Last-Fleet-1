using System;
using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using UnityEngine;

namespace LastFleet.Galaxy
{
    /// <summary>
    /// Blockout star map: one sphere per system, the jump range around the fleet's current system
    /// and lines to every system reachable from it with one jump. Selecting another system only
    /// marks it; the range and the lines stay on the current system. Holds no game state besides
    /// the current system and the selection.
    /// </summary>
    public sealed class GalaxyMapView : MonoBehaviour
    {
        const int RingSegmentCount = 96;
        const float CurrentMarkerScale = 1.6f;
        const float HighlightRingDiameterFactor = 2.6f;

        [SerializeField] Material _markerMaterial;
        [SerializeField] Material _lineMaterial;
        [SerializeField, Min(0.01f)] float _unitsPerLightYear = 1f;
        [SerializeField, Min(0.1f)] float _markerDiameterUnits = 5f;
        [SerializeField, Min(0.01f)] float _lineWidthUnits = 1.2f;

        [Header("Colors")]
        [SerializeField] Color _systemColor = new Color(0.7f, 0.75f, 0.85f);
        [SerializeField] Color _startSystemColor = new Color(0.3f, 1f, 0.45f);
        [SerializeField] Color _currentSystemColor = new Color(1f, 1f, 1f);
        [SerializeField] Color _selectedSystemColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] Color _reachableSystemColor = new Color(0.4f, 0.75f, 1f);
        [SerializeField] Color _jumpLineColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] Color _highlightColor = new Color(1f, 0.35f, 0.85f);

        readonly List<Renderer> _markers = new List<Renderer>();
        readonly List<LineRenderer> _jumpLines = new List<LineRenderer>();
        readonly List<LineRenderer> _highlightRings = new List<LineRenderer>();
        LineRenderer _jumpRangeRing;
        MaterialPropertyBlock _propertyBlock;
        GalaxyLayout _galaxy;

        public GalaxyLayout Galaxy => _galaxy;
        /// <summary>Where the fleet is: the origin of the jump range and lines.</summary>
        public int CurrentSystemIndex { get; private set; } = -1;

        /// <summary>The marked system, e.g. the planned jump target; the current system when nothing else is marked.</summary>
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
            SetCurrentSystem(galaxy.StartSystemIndex);
        }

        /// <summary>The fleet arrived in a system: range and lines move there, and it becomes the selection.</summary>
        public void SetCurrentSystem(int systemIndex)
        {
            ValidateIndex(systemIndex);
            CurrentSystemIndex = systemIndex;
            SelectedSystemIndex = systemIndex;
            RefreshMarkerColors();
            RefreshJumpLines();
        }

        /// <summary>Marks a system without moving the jump range away from the current system.</summary>
        public void Select(int systemIndex)
        {
            ValidateIndex(systemIndex);
            SelectedSystemIndex = systemIndex;
            RefreshMarkerColors();
        }

        void ValidateIndex(int systemIndex)
        {
            if (_galaxy == null)
            {
                throw new InvalidOperationException("Show a galaxy before choosing a system.");
            }
            if (systemIndex < 0 || systemIndex >= _galaxy.Systems.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(systemIndex));
            }
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

        /// <summary>
        /// Rings systems that hold something for the player, e.g. a quest step; replaces the earlier set.
        /// The map does not know why a system is highlighted.
        /// </summary>
        public void SetHighlightedSystems(IReadOnlyList<int> systemIndices)
        {
            if (_galaxy == null)
            {
                return;
            }
            while (_highlightRings.Count < systemIndices.Count)
            {
                LineRenderer ring = BlockoutPrimitives.CreateLine(transform, _lineMaterial, "Highlight Ring", _lineWidthUnits);
                BlockoutPrimitives.SetColor(ring, _highlightColor, _propertyBlock);
                _highlightRings.Add(ring);
            }
            for (int i = 0; i < _highlightRings.Count; i++)
            {
                bool used = i < systemIndices.Count;
                _highlightRings[i].gameObject.SetActive(used);
                if (used)
                {
                    ValidateIndex(systemIndices[i]);
                    BlockoutPrimitives.SetCircle(_highlightRings[i], _markers[systemIndices[i]].transform.localPosition,
                        _markerDiameterUnits * HighlightRingDiameterFactor * 0.5f, RingSegmentCount);
                }
            }
        }

        public void Clear()
        {
            BlockoutPrimitives.DestroyChildren(transform);
            _markers.Clear();
            _jumpLines.Clear();
            _highlightRings.Clear();
            _jumpRangeRing = null;
            _galaxy = null;
            CurrentSystemIndex = -1;
            SelectedSystemIndex = -1;
            ExtentUnits = 0f;
        }

        void RefreshMarkerColors()
        {
            IReadOnlyList<int> reachable = _galaxy.GetNeighbours(CurrentSystemIndex);
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
                if (i == CurrentSystemIndex)
                {
                    color = _currentSystemColor;
                }
                if (i == SelectedSystemIndex && i != CurrentSystemIndex)
                {
                    color = _selectedSystemColor;
                }
                BlockoutPrimitives.SetColor(_markers[i], color, _propertyBlock);
                // The fleet's system stands out by size as well, so the marked target cannot hide it.
                _markers[i].transform.localScale = Vector3.one * (_markerDiameterUnits * (i == CurrentSystemIndex ? CurrentMarkerScale : 1f));
            }
        }

        void RefreshJumpLines()
        {
            Vector3 origin = _markers[CurrentSystemIndex].transform.localPosition;
            BlockoutPrimitives.SetCircle(_jumpRangeRing, origin, _galaxy.JumpRangeLightYears * _unitsPerLightYear, RingSegmentCount);

            IReadOnlyList<int> reachable = _galaxy.GetNeighbours(CurrentSystemIndex);
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
