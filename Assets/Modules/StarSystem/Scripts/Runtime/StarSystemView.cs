using System;
using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using LastFleet.Shared.Randomness;
using UnityEngine;

namespace LastFleet.StarSystem
{
    public enum PointOfInterestMarkerStyle
    {
        Unexplored,
        Explored
    }

    /// <summary>
    /// Blockout map of a star system: star, planets with orbits, moons, asteroid belts, stations and a
    /// ring marker for every point of interest. Distances are compressed with a square root and bodies
    /// are enlarged, so inner and outer orbits stay readable. This is a map, not the ship-scale zone view.
    /// </summary>
    public sealed class StarSystemView : MonoBehaviour
    {
        const int OrbitSegmentCount = 128;
        const int MarkerSegmentCount = 48;
        const ulong AsteroidPlacementSalt = 1000UL;
        const float BeltMarkerRadiusUnits = 6f;
        const float MarkerMarginUnits = 1.5f;

        [SerializeField] Material _bodyMaterial;
        [SerializeField] Material _glowMaterial;

        [Header("Scale")]
        [SerializeField, Min(1f)] float _unitsPerSqrtAU = 60f;
        [Tooltip("Scale for the distance of moons and orbital stations from their enlarged planet.")]
        [SerializeField, Min(1f)] float _satelliteUnitsPerAU = 600f;
        [SerializeField, Min(0.1f)] float _starDiameterUnits = 24f;
        [SerializeField, Min(0.1f)] float _earthDiameterUnits = 4f;
        [SerializeField, Min(0.1f)] float _moonDiameterUnits = 1.2f;
        [SerializeField, Min(0.1f)] float _stationSizeUnits = 1.5f;
        [SerializeField, Min(0.05f)] float _asteroidSizeUnits = 0.8f;
        [SerializeField, Min(1)] int _asteroidsPerBelt = 180;
        [SerializeField, Min(0.01f)] float _lineWidthUnits = 0.5f;
        [Tooltip("Brightness of the star light at half the system extent.")]
        [SerializeField, Min(0f)] float _starLightBrightness = 1.2f;

        [Header("Colors")]
        [SerializeField] Color _orbitColor = new Color(0.25f, 0.4f, 0.6f);
        [SerializeField] Color _stationColor = new Color(0.9f, 0.9f, 0.95f);
        [SerializeField] Color _fuelMarkerColor = new Color(1f, 0.55f, 0.15f);
        [SerializeField] Color _jumpPointColor = new Color(0.5f, 1f, 0.6f);
        [SerializeField] Color _unexploredMarkerColor = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] Color _exploredMarkerColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] Color _selectedMarkerColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField, Min(1f)] float _starGlowIntensity = 4f;

        readonly List<LineRenderer> _markers = new List<LineRenderer>();
        readonly List<PointOfInterestMarkerStyle> _markerStyles = new List<PointOfInterestMarkerStyle>();
        MaterialPropertyBlock _propertyBlock;

        public StarSystemLayout Layout { get; private set; }
        public float ExtentUnits { get; private set; }
        public int SelectedPointOfInterestIndex { get; private set; } = -1;

        void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (_bodyMaterial == null || _glowMaterial == null)
            {
                Debug.LogError("StarSystemView needs a body and a glow material.", this);
            }
        }

        public void Show(StarSystemLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            Clear();
            Layout = layout;
            ExtentUnits = ComputeExtentUnits(layout);

            CreateStar(layout.Star);
            foreach (Planet planet in layout.Planets)
            {
                CreatePlanet(planet);
            }
            for (int i = 0; i < layout.AsteroidBelts.Count; i++)
            {
                CreateBelt(layout.AsteroidBelts[i], DeterministicRandom.DeriveSeed(layout.Seed, AsteroidPlacementSalt + (ulong)i));
            }
            foreach (Station station in layout.Stations)
            {
                CreateStation(station);
            }
            CreateJumpPointBeacon(layout.PointsOfInterest[layout.ArrivalPointOfInterestIndex]);
            CreateMarkers(layout);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Clear()
        {
            BlockoutPrimitives.DestroyChildren(transform);
            _markers.Clear();
            _markerStyles.Clear();
            Layout = null;
            ExtentUnits = 0f;
            SelectedPointOfInterestIndex = -1;
        }

        public void SelectPointOfInterest(int index)
        {
            ValidatePointOfInterestIndex(index);
            int previous = SelectedPointOfInterestIndex;
            SelectedPointOfInterestIndex = index;
            if (previous >= 0)
            {
                RefreshMarker(previous);
            }
            RefreshMarker(index);
        }

        public void SetMarkerStyle(int index, PointOfInterestMarkerStyle style)
        {
            ValidatePointOfInterestIndex(index);
            _markerStyles[index] = style;
            RefreshMarker(index);
        }

        public Vector3 GetPointOfInterestWorldPosition(int index)
        {
            ValidatePointOfInterestIndex(index);
            return transform.TransformPoint(GetPointOfInterestLocalPosition(Layout.PointsOfInterest[index]));
        }

        /// <summary>Finds the point of interest closest to a screen position, so small bodies stay clickable.</summary>
        public bool TryPickPointOfInterest(Camera camera, Vector2 screenPosition, float maximumDistancePixels, out int index)
        {
            index = -1;
            if (Layout == null)
            {
                return false;
            }

            float bestDistanceSquared = maximumDistancePixels * maximumDistancePixels;
            for (int i = 0; i < Layout.PointsOfInterest.Count; i++)
            {
                Vector3 screenPoint = camera.WorldToScreenPoint(GetPointOfInterestWorldPosition(i));
                if (screenPoint.z <= 0f)
                {
                    continue;
                }

                float distanceSquared = (new Vector2(screenPoint.x, screenPoint.y) - screenPosition).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    index = i;
                }
            }
            return index >= 0;
        }

        Vector3 GetPointOfInterestLocalPosition(PointOfInterest point)
        {
            return point.IsSatellite ? SatelliteLocalPosition(point.AssociatedPlanet, point.PositionAU) : ToLocalPosition(point.PositionAU);
        }

        Vector3 ToLocalPosition(Vector2 positionAU)
        {
            float radiusAU = positionAU.magnitude;
            if (radiusAU < 1e-6f)
            {
                return Vector3.zero;
            }

            Vector2 direction = positionAU / radiusAU;
            float distance = ToViewDistance(radiusAU);
            return new Vector3(direction.x * distance, 0f, direction.y * distance);
        }

        /// <summary>Bodies are enlarged on the map, so satellites are pushed outside their planet's sphere.</summary>
        Vector3 SatelliteLocalPosition(Planet parent, Vector2 positionAU)
        {
            Vector2 offsetAU = positionAU - parent.PositionAU;
            float offsetLengthAU = offsetAU.magnitude;
            Vector2 direction = offsetLengthAU > 1e-6f ? offsetAU / offsetLengthAU : Vector2.right;
            float distance = PlanetDiameterUnits(parent) * 0.5f + _moonDiameterUnits + offsetLengthAU * _satelliteUnitsPerAU;
            return ToLocalPosition(parent.PositionAU) + new Vector3(direction.x, 0f, direction.y) * distance;
        }

        float ToViewDistance(float radiusAU)
        {
            return _unitsPerSqrtAU * Mathf.Sqrt(radiusAU);
        }

        float PlanetDiameterUnits(Planet planet)
        {
            // Square root keeps gas giants large without dwarfing the orbit spacing.
            return _earthDiameterUnits * Mathf.Sqrt(planet.RadiusEarth);
        }

        float MarkerRadiusUnits(PointOfInterest point)
        {
            return point.Kind switch
            {
                PointOfInterestKind.Star => _starDiameterUnits * 0.5f + MarkerMarginUnits * 2f,
                PointOfInterestKind.Planet => PlanetDiameterUnits(point.AssociatedPlanet) * 0.5f + MarkerMarginUnits,
                PointOfInterestKind.Moon => _moonDiameterUnits * 0.5f + MarkerMarginUnits,
                PointOfInterestKind.Station => _stationSizeUnits * 0.5f + MarkerMarginUnits,
                _ => BeltMarkerRadiusUnits
            };
        }

        float ComputeExtentUnits(StarSystemLayout layout)
        {
            float outermostAU = 0f;
            foreach (Planet planet in layout.Planets)
            {
                outermostAU = Mathf.Max(outermostAU, planet.OrbitRadiusAU);
            }
            foreach (AsteroidBelt belt in layout.AsteroidBelts)
            {
                outermostAU = Mathf.Max(outermostAU, belt.OuterRadiusAU);
            }
            foreach (PointOfInterest point in layout.PointsOfInterest)
            {
                outermostAU = Mathf.Max(outermostAU, point.PositionAU.magnitude);
            }
            return ToViewDistance(outermostAU) + _starDiameterUnits;
        }

        void CreateStar(Star star)
        {
            Renderer body = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, transform, _glowMaterial, "Star");
            body.transform.localScale = Vector3.one * _starDiameterUnits;
            BlockoutPrimitives.SetColor(body, star.Color * _starGlowIntensity, _propertyBlock);

            // Inverse-square falloff: intensity is chosen so the light has the configured brightness
            // at half the system extent.
            Light starLight = body.gameObject.AddComponent<Light>();
            starLight.type = LightType.Point;
            starLight.color = star.Color;
            starLight.range = ExtentUnits * 1.5f;
            float halfExtent = ExtentUnits * 0.5f;
            starLight.intensity = _starLightBrightness * halfExtent * halfExtent;
            starLight.shadows = LightShadows.None;
        }

        void CreatePlanet(Planet planet)
        {
            LineRenderer orbit = BlockoutPrimitives.CreateLine(transform, _glowMaterial, $"{planet.Name} Orbit", _lineWidthUnits);
            BlockoutPrimitives.SetCircle(orbit, Vector3.zero, ToViewDistance(planet.OrbitRadiusAU), OrbitSegmentCount);
            BlockoutPrimitives.SetColor(orbit, _orbitColor, _propertyBlock);

            float diameter = PlanetDiameterUnits(planet);
            Vector3 planetPosition = ToLocalPosition(planet.PositionAU);
            Renderer body = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, transform, _bodyMaterial, planet.Name);
            body.transform.localPosition = planetPosition;
            body.transform.localScale = Vector3.one * diameter;
            BlockoutPrimitives.SetColor(body, CelestialPalette.PlanetColor(planet.Kind), _propertyBlock);

            if (planet.IsFuelSource)
            {
                LineRenderer fuelRing = BlockoutPrimitives.CreateLine(transform, _glowMaterial, $"{planet.Name} Fuel", _lineWidthUnits);
                BlockoutPrimitives.SetCircle(fuelRing, planetPosition, diameter * 0.75f, MarkerSegmentCount);
                BlockoutPrimitives.SetColor(fuelRing, _fuelMarkerColor, _propertyBlock);
            }

            foreach (Moon moon in planet.Moons)
            {
                Vector2 moonPositionAU = planet.PositionAU + OrbitMath.PositionOnOrbit(moon.OrbitRadiusAU, moon.OrbitAngleDegrees);
                Renderer moonBody = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, transform, _bodyMaterial, moon.Name);
                moonBody.transform.localPosition = SatelliteLocalPosition(planet, moonPositionAU);
                moonBody.transform.localScale = Vector3.one * _moonDiameterUnits;
                BlockoutPrimitives.SetColor(moonBody, CelestialPalette.MoonColor, _propertyBlock);
            }
        }

        void CreateBelt(AsteroidBelt belt, ulong placementSeed)
        {
            GameObject beltRoot = new GameObject(belt.Name);
            beltRoot.transform.SetParent(transform, false);
            Color color = CelestialPalette.BeltColor(belt.Composition);
            DeterministicRandom random = new DeterministicRandom(placementSeed);

            for (int i = 0; i < _asteroidsPerBelt; i++)
            {
                Vector2 positionAU = OrbitMath.PositionOnOrbit(random.Range(belt.InnerRadiusAU, belt.OuterRadiusAU), random.AngleDegrees());
                Renderer asteroid = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Cube, beltRoot.transform, _bodyMaterial, "Asteroid");
                asteroid.transform.localPosition = ToLocalPosition(positionAU) + Vector3.up * random.Range(-1f, 1f);
                asteroid.transform.localRotation = Quaternion.Euler(random.AngleDegrees(), random.AngleDegrees(), random.AngleDegrees());
                asteroid.transform.localScale = Vector3.one * _asteroidSizeUnits * random.Range(0.5f, 1.5f);
                BlockoutPrimitives.SetColor(asteroid, color, _propertyBlock);
            }
        }

        void CreateStation(Station station)
        {
            Vector3 position = station.OrbitedPlanet != null
                ? SatelliteLocalPosition(station.OrbitedPlanet, station.PositionAU)
                : ToLocalPosition(station.PositionAU);

            Renderer body = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Cube, transform, _glowMaterial, station.Name);
            body.transform.localPosition = position;
            body.transform.localScale = Vector3.one * _stationSizeUnits;
            body.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            BlockoutPrimitives.SetColor(body, _stationColor, _propertyBlock);
        }

        void CreateJumpPointBeacon(PointOfInterest jumpPoint)
        {
            Renderer beacon = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Cube, transform, _glowMaterial, jumpPoint.Name);
            beacon.transform.localPosition = ToLocalPosition(jumpPoint.PositionAU);
            beacon.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            beacon.transform.localScale = Vector3.one * _stationSizeUnits;
            BlockoutPrimitives.SetColor(beacon, _jumpPointColor, _propertyBlock);
        }

        void CreateMarkers(StarSystemLayout layout)
        {
            foreach (PointOfInterest point in layout.PointsOfInterest)
            {
                LineRenderer marker = BlockoutPrimitives.CreateLine(transform, _glowMaterial, $"{point.Name} Marker", _lineWidthUnits);
                BlockoutPrimitives.SetCircle(marker, GetPointOfInterestLocalPosition(point), MarkerRadiusUnits(point), MarkerSegmentCount);
                _markers.Add(marker);
                _markerStyles.Add(PointOfInterestMarkerStyle.Unexplored);
                RefreshMarker(_markers.Count - 1);
            }
        }

        void RefreshMarker(int index)
        {
            Color color = _markerStyles[index] == PointOfInterestMarkerStyle.Explored ? _exploredMarkerColor : _unexploredMarkerColor;
            if (index == SelectedPointOfInterestIndex)
            {
                color = _selectedMarkerColor;
            }
            BlockoutPrimitives.SetColor(_markers[index], color, _propertyBlock);
        }

        void ValidatePointOfInterestIndex(int index)
        {
            if (Layout == null)
            {
                throw new InvalidOperationException("Show a star system before using its points of interest.");
            }
            if (index < 0 || index >= Layout.PointsOfInterest.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
