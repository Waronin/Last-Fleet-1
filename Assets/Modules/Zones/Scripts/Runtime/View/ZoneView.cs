using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using LastFleet.Space;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastFleet.Zones
{
    /// <summary>
    /// Blockout presentation of one revealed zone: grid, resource deposits, stations and the
    /// backdrop body. Reads the zone's run state; call <see cref="Refresh"/> after deposits change.
    /// </summary>
    public sealed class ZoneView : MonoBehaviour
    {
        const float GasCloudHeightFactor = 0.35f;
        const float BackdropGlowIntensity = 4f;

        static readonly Vector3 PlanetBackdropPosition = new Vector3(0f, -1100f, 900f);
        static readonly Vector3 MoonBackdropPosition = new Vector3(0f, -600f, 700f);
        static readonly Vector3 StarBackdropPosition = new Vector3(0f, -500f, 1200f);
        const float PlanetBackdropDiameter = 1400f;
        const float MoonBackdropDiameter = 500f;
        const float StarBackdropDiameter = 700f;

        readonly List<Renderer> _depositRenderers = new List<Renderer>();
        MaterialPropertyBlock _propertyBlock;

        public ZoneRecord Record { get; private set; }
        public SpaceZone Zone { get; private set; }

        public void Build(ZoneRecord record, Material gridMaterial, Material bodyMaterial, Material glowMaterial)
        {
            Record = record;
            _propertyBlock = new MaterialPropertyBlock();
            ZoneContents contents = record.Contents;

            Renderer grid = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Quad, transform, gridMaterial, "Grid");
            grid.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Zone = gameObject.AddComponent<SpaceZone>();
            Zone.Initialize(record.PointOfInterest.Name, contents.RadiusKlicks, grid);

            CreateBackdrop(contents.Backdrop, bodyMaterial, glowMaterial);
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                _depositRenderers.Add(CreateDeposit(deposit, bodyMaterial));
            }
            foreach (ZoneStation station in contents.Stations)
            {
                CreateStation(station, glowMaterial);
            }
            Refresh();
        }

        /// <summary>Updates deposit visuals from their remaining amounts; depleted deposits disappear.</summary>
        public void Refresh()
        {
            IReadOnlyList<ResourceDeposit> deposits = Record.Contents.Deposits;
            for (int i = 0; i < deposits.Count; i++)
            {
                ResourceDeposit deposit = deposits[i];
                Renderer renderer = _depositRenderers[i];
                renderer.gameObject.SetActive(!deposit.IsDepleted);
                if (deposit.IsDepleted)
                {
                    continue;
                }

                // Volume shrinks with the remaining amount, so the radius follows its cube root.
                float diameter = deposit.RadiusUnits * 2f * Mathf.Pow(deposit.RemainingFraction, 1f / 3f);
                float height = deposit.Shape == DepositShape.GasCloud ? diameter * GasCloudHeightFactor : diameter;
                renderer.transform.localScale = new Vector3(diameter, height, diameter);
            }
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

        Renderer CreateDeposit(ResourceDeposit deposit, Material bodyMaterial)
        {
            PrimitiveType primitive = deposit.Shape == DepositShape.GasCloud ? PrimitiveType.Sphere : PrimitiveType.Cube;
            Renderer renderer = BlockoutPrimitives.CreatePrimitive(primitive, transform, bodyMaterial, $"{deposit.Resource} {deposit.Shape}");
            renderer.transform.localPosition = deposit.LocalPosition;
            if (deposit.Shape == DepositShape.Asteroid)
            {
                // Stable pseudo-random tilt from the position, so a rebuilt zone looks the same.
                Vector3 p = deposit.LocalPosition;
                renderer.transform.localRotation = Quaternion.Euler(p.x * 7.3f % 360f, p.z * 5.1f % 360f, p.y * 11.7f % 360f);
            }
            BlockoutPrimitives.SetColor(renderer, ResourceColor(deposit.Resource), _propertyBlock);
            return renderer;
        }

        void CreateStation(ZoneStation station, Material glowMaterial)
        {
            Renderer renderer = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Cube, transform, glowMaterial, station.Name);
            renderer.transform.localPosition = station.LocalPosition;
            renderer.transform.localScale = new Vector3(station.SizeUnits, station.SizeUnits * 0.5f, station.SizeUnits);
            BlockoutPrimitives.SetColor(renderer, new Color(0.9f, 0.9f, 0.95f), _propertyBlock);
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

        static Color ResourceColor(ResourceKind resource)
        {
            return resource switch
            {
                ResourceKind.Ore => new Color(0.6f, 0.5f, 0.4f),
                ResourceKind.Ice => new Color(0.75f, 0.9f, 1f),
                ResourceKind.Gas => new Color(1f, 0.6f, 0.25f),
                _ => Color.magenta
            };
        }
    }
}
