using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using LastFleet.Shared.Randomness;
using LastFleet.Space;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Zones
{
    /// <summary>
    /// Blockout presentation of one revealed zone: grid, resource deposits, station models and the
    /// backdrop body. Reads the zone's run state; call <see cref="Refresh"/> after deposits change.
    /// </summary>
    public sealed class ZoneView : MonoBehaviour
    {
        const float BackdropGlowIntensity = 4f;
        const ulong DepositVisualSalt = 2000UL;

        static readonly Vector3 PlanetBackdropPosition = new Vector3(0f, -1100f, 900f);
        static readonly Vector3 MoonBackdropPosition = new Vector3(0f, -600f, 700f);
        static readonly Vector3 StarBackdropPosition = new Vector3(0f, -500f, 1200f);
        const float PlanetBackdropDiameter = 1400f;
        const float MoonBackdropDiameter = 500f;
        const float StarBackdropDiameter = 700f;

        readonly List<DepositView> _depositViews = new List<DepositView>();
        readonly Dictionary<ResourceDeposit, DepositView> _viewByDeposit = new Dictionary<ResourceDeposit, DepositView>();
        readonly Dictionary<ZoneStation, Transform> _viewByStation = new Dictionary<ZoneStation, Transform>();
        MaterialPropertyBlock _propertyBlock;
        Material _bodyMaterial;
        Material _glowMaterial;

        public ZoneRecord Record { get; private set; }
        public SpaceZone Zone { get; private set; }

        public void Build(ZoneRecord record, Material gridMaterial, Material bodyMaterial, Material glowMaterial, StationViewSource stationViews)
        {
            Record = record;
            _propertyBlock = new MaterialPropertyBlock();
            ZoneContents contents = record.Contents;

            Renderer grid = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Quad, transform, gridMaterial, "Grid");
            grid.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Zone = gameObject.AddComponent<SpaceZone>();
            Zone.Initialize(record.PointOfInterest.Name, contents.RadiusKlicks, grid);

            _bodyMaterial = bodyMaterial;
            _glowMaterial = glowMaterial;
            CreateBackdrop(contents.Backdrop, bodyMaterial, glowMaterial);
            for (int i = 0; i < contents.Deposits.Count; i++)
            {
                CreateDepositView(contents.Deposits[i], i);
            }
            foreach (ZoneStation station in contents.Stations)
            {
                _viewByStation.Add(station, stationViews.CreateView(station, transform));
            }
        }

        /// <summary>Adds a deposit that appeared during the run, e.g. a wreck, to the zone's state and shows it.</summary>
        public void AddDeposit(ResourceDeposit deposit)
        {
            Record.Contents.AddDeposit(deposit);
            CreateDepositView(deposit, Record.Contents.Deposits.Count - 1);
        }

        void CreateDepositView(ResourceDeposit deposit, int index)
        {
            GameObject depositObject = new GameObject($"{deposit.Shape} {index + 1}");
            depositObject.transform.SetParent(transform, false);
            DepositView view = depositObject.AddComponent<DepositView>();
            // Seeded from the zone and the index, so a rebuilt zone shows the same rocks and wrecks.
            view.Build(deposit, DeterministicRandom.DeriveSeed(Record.Seed, DepositVisualSalt + (ulong)index), _bodyMaterial, _glowMaterial);
            _depositViews.Add(view);
            _viewByDeposit.Add(deposit, view);
        }

        /// <summary>Updates deposit visuals from their remaining amounts and scan state; depleted deposits disappear.</summary>
        public void Refresh()
        {
            foreach (DepositView view in _depositViews)
            {
                view.Refresh();
            }
        }

        public void RefreshDeposit(ResourceDeposit deposit)
        {
            if (_viewByDeposit.TryGetValue(deposit, out DepositView view))
            {
                view.Refresh();
            }
        }

        /// <summary>
        /// Finds the deposit under a screen position: inside its projected outline, or within a few
        /// pixels of its center so small rocks stay clickable.
        /// </summary>
        public bool TryPickDeposit(Camera camera, Vector2 screenPosition, float minimumRadiusPixels, out ResourceDeposit deposit)
        {
            deposit = null;
            float bestDistance = float.MaxValue;
            foreach (ResourceDeposit candidate in Record.Contents.Deposits)
            {
                if (candidate.IsDepleted)
                {
                    continue;
                }

                Vector3 center = transform.TransformPoint(candidate.LocalPosition);
                Vector3 screenCenter = camera.WorldToScreenPoint(center);
                if (screenCenter.z <= 0f)
                {
                    continue;
                }

                Vector3 screenEdge = camera.WorldToScreenPoint(center + camera.transform.right * candidate.RadiusUnits);
                float radiusPixels = Mathf.Max(minimumRadiusPixels, Vector2.Distance(screenCenter, screenEdge));
                float distance = Vector2.Distance(new Vector2(screenCenter.x, screenCenter.y), screenPosition);
                if (distance <= radiusPixels && distance < bestDistance)
                {
                    bestDistance = distance;
                    deposit = candidate;
                }
            }
            return deposit != null;
        }

        /// <summary>The station whose footprint contains the point on the zone plane under a screen position.</summary>
        public bool TryPickStation(Camera camera, Vector2 screenPosition, out ZoneStation station)
        {
            station = null;
            if (!Zone.TryRaycastPlane(camera.ScreenPointToRay(screenPosition), out Vector3 worldPoint))
            {
                return false;
            }

            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            float bestDistance = float.MaxValue;
            foreach (ZoneStation candidate in Record.Contents.Stations)
            {
                float distance = new Vector2(localPoint.x - candidate.LocalPosition.x, localPoint.z - candidate.LocalPosition.z).magnitude;
                if (distance <= candidate.FootprintRadiusUnits && distance < bestDistance)
                {
                    bestDistance = distance;
                    station = candidate;
                }
            }
            return station != null;
        }

        /// <summary>The model of a station in this zone, e.g. to find its docking bays.</summary>
        public bool TryGetStationView(ZoneStation station, out Transform view)
        {
            return _viewByStation.TryGetValue(station, out view);
        }

        /// <summary>Returns the index of the non-depleted deposit closest to a world point, or -1.</summary>
        public int FindNearestDeposit(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            IReadOnlyList<ResourceDeposit> deposits = Record.Contents.Deposits;
            int nearestIndex = -1;
            float nearestDistanceSquared = float.MaxValue;
            for (int i = 0; i < deposits.Count; i++)
            {
                if (deposits[i].IsDepleted)
                {
                    continue;
                }

                float distanceSquared = (deposits[i].LocalPosition - localPoint).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestIndex = i;
                }
            }
            return nearestIndex;
        }

        void CreateBackdrop(ZoneBackdrop backdrop, Material bodyMaterial, Material glowMaterial)
        {
            switch (backdrop.Kind)
            {
                case ZoneBackdropKind.Planet:
                    CreateBackdropBody(bodyMaterial, backdrop.Color, PlanetBackdropPosition, PlanetBackdropDiameter);
                    break;
                case ZoneBackdropKind.Moon:
                    CreateBackdropBody(bodyMaterial, backdrop.Color, MoonBackdropPosition, MoonBackdropDiameter);
                    break;
                case ZoneBackdropKind.Star:
                    CreateBackdropBody(glowMaterial, backdrop.Color * BackdropGlowIntensity, StarBackdropPosition, StarBackdropDiameter);
                    break;
                case ZoneBackdropKind.None:
                    break;
            }
        }

        void CreateBackdropBody(Material material, Color color, Vector3 localPosition, float diameter)
        {
            Renderer renderer = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, transform, material, "Backdrop");
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = Vector3.one * diameter;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            BlockoutPrimitives.SetColor(renderer, color, _propertyBlock);
        }
    }
}
