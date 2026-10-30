using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using LastFleet.Shared.Randomness;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// Blockout of one resource deposit. Asteroids are a few overlapping, squashed spheres so they
    /// read as irregular rocks. Unscanned deposits only show their size; a scan reveals the resource
    /// color, the hazard ring and the mining slots. Call <see cref="Refresh"/> after the deposit changes.
    /// </summary>
    public sealed class DepositView : MonoBehaviour
    {
        const int AsteroidLumpCount = 5;
        const int WreckPlateCount = 6;
        const float GasCloudHeightFactor = 0.35f;
        const float HazardRingRadiusFactor = 1.4f;
        const float HazardRingWidthUnits = 0.15f;
        const int HazardRingSegments = 40;
        const float SlotMarkerSizeUnits = 0.18f;

        static readonly Color UnscannedRockColor = new Color(0.42f, 0.4f, 0.38f);
        static readonly Color UnscannedGasColor = new Color(0.55f, 0.4f, 0.3f);
        static readonly Color SlotMarkerColor = new Color(0.2f, 0.6f, 0.8f);

        readonly List<Renderer> _bodyParts = new List<Renderer>();
        readonly List<Renderer> _slotMarkers = new List<Renderer>();
        Transform _body;
        LineRenderer _hazardRing;
        MaterialPropertyBlock _propertyBlock;

        public ResourceDeposit Deposit { get; private set; }

        public void Build(ResourceDeposit deposit, ulong visualSeed, Material bodyMaterial, Material glowMaterial)
        {
            Deposit = deposit;
            _propertyBlock = new MaterialPropertyBlock();
            transform.localPosition = deposit.LocalPosition;

            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            DeterministicRandom random = new DeterministicRandom(visualSeed);
            if (deposit.Shape == DepositShape.Asteroid)
            {
                CreateAsteroidLumps(random, bodyMaterial);
            }
            else if (deposit.Shape == DepositShape.Wreck)
            {
                CreateWreckPlates(random, bodyMaterial);
            }
            else
            {
                _bodyParts.Add(BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, _body, bodyMaterial, "Cloud"));
            }

            _hazardRing = BlockoutPrimitives.CreateLine(transform, glowMaterial, "Hazard", HazardRingWidthUnits);
            BlockoutPrimitives.SetCircle(_hazardRing, Vector3.zero, deposit.RadiusUnits * HazardRingRadiusFactor, HazardRingSegments);
            BlockoutPrimitives.SetColor(_hazardRing, HazardColor(deposit.Hazard), _propertyBlock);

            for (int i = 0; i < deposit.MiningSlotCount; i++)
            {
                Renderer marker = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Cube, transform, glowMaterial, $"Mining Slot {i + 1}");
                marker.transform.localPosition = deposit.GetMiningSlotLocalPosition(i) - deposit.LocalPosition;
                marker.transform.localScale = Vector3.one * SlotMarkerSizeUnits;
                BlockoutPrimitives.SetColor(marker, SlotMarkerColor, _propertyBlock);
                _slotMarkers.Add(marker);
            }
            Refresh();
        }

        /// <summary>Updates size, color and markers from the deposit's remaining amount and scan state.</summary>
        public void Refresh()
        {
            gameObject.SetActive(!Deposit.IsDepleted);
            if (Deposit.IsDepleted)
            {
                return;
            }

            // Volume shrinks with the remaining amount, so the radius follows its cube root.
            float diameter = Deposit.RadiusUnits * 2f * Mathf.Pow(Deposit.RemainingFraction, 1f / 3f);
            float height = Deposit.Shape == DepositShape.GasCloud ? diameter * GasCloudHeightFactor : diameter;
            _body.localScale = new Vector3(diameter, height, diameter);

            Color color = Deposit.IsScanned ? ResourceColor(Deposit.Resource)
                : Deposit.Shape == DepositShape.GasCloud ? UnscannedGasColor : UnscannedRockColor;
            foreach (Renderer part in _bodyParts)
            {
                BlockoutPrimitives.SetColor(part, color, _propertyBlock);
            }

            _hazardRing.gameObject.SetActive(Deposit.IsScanned && Deposit.Hazard != DepositHazard.None);
            foreach (Renderer marker in _slotMarkers)
            {
                marker.gameObject.SetActive(Deposit.IsScanned);
            }
        }

        /// <summary>Lumps live in a unit body that is scaled to the deposit size, so depletion shrinks all of them.</summary>
        void CreateAsteroidLumps(DeterministicRandom random, Material bodyMaterial)
        {
            _body.localRotation = Quaternion.Euler(random.AngleDegrees(), random.AngleDegrees(), random.AngleDegrees());
            for (int i = 0; i < AsteroidLumpCount; i++)
            {
                Renderer lump = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Sphere, _body, bodyMaterial, $"Lump {i + 1}");
                lump.transform.localPosition = i == 0 ? Vector3.zero : random.InsideUnitSphere().normalized * random.Range(0.3f, 0.45f);
                lump.transform.localRotation = Quaternion.Euler(random.AngleDegrees(), random.AngleDegrees(), random.AngleDegrees());
                lump.transform.localScale = i == 0
                    ? new Vector3(0.9f, random.Range(0.5f, 0.7f), random.Range(0.65f, 0.85f))
                    : new Vector3(random.Range(0.35f, 0.6f), random.Range(0.3f, 0.5f), random.Range(0.35f, 0.6f));
                _bodyParts.Add(lump);
            }
        }

        /// <summary>Torn hull plates at odd angles, so wreckage reads differently from a rock.</summary>
        void CreateWreckPlates(DeterministicRandom random, Material bodyMaterial)
        {
            _body.localRotation = Quaternion.Euler(random.AngleDegrees(), random.AngleDegrees(), random.AngleDegrees());
            for (int i = 0; i < WreckPlateCount; i++)
            {
                Renderer plate = BlockoutPrimitives.CreatePrimitive(PrimitiveType.Cube, _body, bodyMaterial, $"Plate {i + 1}");
                plate.transform.localPosition = random.InsideUnitSphere() * 0.3f;
                plate.transform.localRotation = Quaternion.Euler(random.AngleDegrees(), random.AngleDegrees(), random.AngleDegrees());
                plate.transform.localScale = new Vector3(random.Range(0.3f, 0.7f), random.Range(0.04f, 0.1f), random.Range(0.2f, 0.5f));
                _bodyParts.Add(plate);
            }
        }

        static Color ResourceColor(ResourceKind resource)
        {
            return resource switch
            {
                ResourceKind.Ore => new Color(0.6f, 0.45f, 0.32f),
                ResourceKind.Ice => new Color(0.75f, 0.9f, 1f),
                ResourceKind.Gas => new Color(1f, 0.6f, 0.25f),
                ResourceKind.Metal => new Color(0.7f, 0.74f, 0.8f),
                _ => Color.magenta
            };
        }

        static Color HazardColor(DepositHazard hazard)
        {
            return hazard switch
            {
                DepositHazard.Brittle => new Color(1.4f, 1.4f, 1.4f),
                DepositHazard.Radioactive => new Color(0.4f, 1.8f, 0.3f),
                DepositHazard.GasPockets => new Color(1.8f, 0.4f, 0.2f),
                _ => Color.clear
            };
        }
    }
}
