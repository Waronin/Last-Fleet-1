using System;
using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// Holds the views of all revealed zones of the star system the fleet is in. Zones are laid out
    /// on a grid far apart in world space, so they can be active at the same time without seeing each
    /// other. Unrevealed zones have no GameObjects at all.
    /// </summary>
    public sealed class ZoneWorld : MonoBehaviour
    {
        [SerializeField] Material _gridMaterial;
        [SerializeField] Material _bodyMaterial;
        [SerializeField] Material _glowMaterial;
        [Tooltip("Creates the station models; they live in the Stations module.")]
        [SerializeField] StationViewSource _stationViews;
        [Tooltip("Directional light for the zone being viewed; it shines from the system's star.")]
        [SerializeField] Light _zoneSun;
        [Tooltip("Distance between zone centers. Must exceed a zone plus its backdrop and the camera far clip.")]
        [SerializeField, Min(100f)] float _zoneSpacingUnits = 4000f;
        [SerializeField, Min(1)] int _columns = 6;
        [Tooltip("Downward tilt of the zone sun so the zone plane is lit from above.")]
        [SerializeField, Range(0f, 1f)] float _sunDownwardTilt = 0.4f;

        readonly Dictionary<int, ZoneView> _views = new Dictionary<int, ZoneView>();

        public SystemZones CurrentSystem { get; private set; }
        public Light ZoneSun => _zoneSun;

        /// <summary>A zone was revealed for the first time in this run, by a scan or a ship arriving.</summary>
        public event Action<ZoneView> ZoneRevealed;

        void Awake()
        {
            if (_gridMaterial == null || _bodyMaterial == null || _glowMaterial == null || _zoneSun == null || _stationViews == null)
            {
                Debug.LogError("ZoneWorld needs grid, body and glow materials, a zone sun and the station views.", this);
            }
            SetSunEnabled(false);
        }

        /// <summary>Builds the views of every already revealed zone, e.g. when the fleet arrives or returns.</summary>
        public void Show(SystemZones system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            Clear();
            CurrentSystem = system;
            foreach (ZoneRecord zone in system.Zones)
            {
                if (zone.IsRevealed)
                {
                    BuildView(zone);
                }
            }
        }

        /// <summary>
        /// Reveals a zone and builds its view. Already revealed zones keep their view and state.
        /// Raises <see cref="ZoneRevealed"/> only when the zone was not revealed before.
        /// </summary>
        public ZoneView Reveal(int pointOfInterestIndex)
        {
            ZoneRecord zone = GetZone(pointOfInterestIndex);
            if (_views.TryGetValue(pointOfInterestIndex, out ZoneView existing))
            {
                return existing;
            }

            bool wasRevealed = zone.IsRevealed;
            zone.Reveal();
            ZoneView view = BuildView(zone);
            if (!wasRevealed)
            {
                ZoneRevealed?.Invoke(view);
            }
            return view;
        }

        public bool TryGetView(int pointOfInterestIndex, out ZoneView view)
        {
            return _views.TryGetValue(pointOfInterestIndex, out view);
        }

        /// <summary>Points the zone sun from the star towards the given zone.</summary>
        public void LightZone(ZoneView view)
        {
            Vector2 positionAU = view.Record.PointOfInterest.PositionAU;
            Vector3 awayFromStar = positionAU.sqrMagnitude > 1e-8f
                ? new Vector3(positionAU.x, 0f, positionAU.y).normalized
                : Vector3.forward;
            _zoneSun.transform.rotation = Quaternion.LookRotation(awayFromStar + Vector3.down * _sunDownwardTilt);
            SetSunEnabled(true);
        }

        public void SetSunEnabled(bool enabled)
        {
            if (_zoneSun != null)
            {
                _zoneSun.enabled = enabled;
            }
        }

        /// <summary>Destroys all zone views, e.g. when the fleet leaves the system. Run state is kept.</summary>
        public void Clear()
        {
            BlockoutPrimitives.DestroyChildren(transform);
            _views.Clear();
            CurrentSystem = null;
            SetSunEnabled(false);
        }

        ZoneView BuildView(ZoneRecord zone)
        {
            GameObject viewObject = new GameObject($"Zone {zone.PointOfInterest.Name}");
            viewObject.transform.SetParent(transform, false);
            viewObject.transform.localPosition = ZoneOrigin(zone.PointOfInterestIndex);

            ZoneView view = viewObject.AddComponent<ZoneView>();
            view.Build(zone, _gridMaterial, _bodyMaterial, _glowMaterial, _stationViews);
            _views.Add(zone.PointOfInterestIndex, view);
            return view;
        }

        Vector3 ZoneOrigin(int pointOfInterestIndex)
        {
            int column = pointOfInterestIndex % _columns;
            int row = pointOfInterestIndex / _columns;
            return new Vector3(column * _zoneSpacingUnits, 0f, row * _zoneSpacingUnits);
        }

        ZoneRecord GetZone(int pointOfInterestIndex)
        {
            if (CurrentSystem == null)
            {
                throw new InvalidOperationException("Show a star system before revealing zones.");
            }
            if (pointOfInterestIndex < 0 || pointOfInterestIndex >= CurrentSystem.Zones.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(pointOfInterestIndex));
            }
            return CurrentSystem.Zones[pointOfInterestIndex];
        }
    }
}
