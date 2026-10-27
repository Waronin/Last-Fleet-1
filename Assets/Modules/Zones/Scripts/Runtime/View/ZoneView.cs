using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using LastFleet.Shared.Randomness;
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
        const float BackdropGlowIntensity = 4f;
        const ulong DepositVisualSalt = 2000UL;

        static readonly Vector3 PlanetBackdropPosition = new Vector3(0f, -1100f, 900f);
        static readonly Vector3 MoonBackdropPosition = new Vector3(0f, -600f, 700f);
        static readonly Vector3 StarBackdropPosition = new Vector3(0f, -500f, 1200f);
        const float PlanetBackdropDiameter = 1400f;
        const float MoonBackdropDiameter = 500f;
        const float StarBackdropDiameter = 700f;

        readonly List<DepositView> _depositViews = new List<DepositView>();
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
            for (int i = 0; i < contents.Deposits.Count; i++)
            {
                ResourceDeposit deposit = contents.Deposits[i];
                GameObject depositObject = new GameObject($"{deposit.Shape} {i + 1}");
                depositObject.transform.SetParent(transform, false);
                DepositView view = depositObject.AddComponent<DepositView>();
                // Seeded from the zone, so a rebuilt zone shows the same rocks.
                view.Build(deposit, DeterministicRandom.DeriveSeed(record.Seed, DepositVisualSalt + (ulong)i), bodyMaterial, glowMaterial);
                _depositViews.Add(view);
            }
            foreach (ZoneStation station in contents.Stations)
            {
                CreateStation(station, glowMaterial);
            }
        }

        /// <summary>Updates deposit visuals from their remaining amounts and scan state; depleted deposits disappear.</summary>
        public void Refresh()
        {
            foreach (DepositView view in _depositViews)
            {
                view.Refresh();
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
    }
}
