using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Leaves salvageable wreckage where the ship is destroyed (GDD: destroyed ships leave wreckage
    /// that can yield metal; staying to collect it risks the main attack). Each piece becomes a
    /// deposit of the zone, so harvesters salvage it like a rock and it stays for the rest of the run.
    /// Small ships leave one piece; capital ships break up into several, spread around the spot
    /// (<see cref="WreckScatter"/>). Ships that are captured or left behind leave nothing.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipDurability))]
    public sealed class ShipWreckage : MonoBehaviour
    {
        [Tooltip("Metal in each piece.")]
        [SerializeField, Min(1f)] float _metalAmount = 25f;
        [SerializeField, Min(1)] int _pieceCount = 1;
        [Tooltip("How far the pieces spread from where the ship was; enough that the pieces and their salvage slots keep apart.")]
        [SerializeField, Min(0f)] float _scatterRadiusUnits;
        [Tooltip("Size of each piece; the size also sets how many harvesters can salvage it at once.")]
        [SerializeField, Min(0.1f)] float _pieceRadiusUnits = ResourceDeposit.WreckRadiusUnits;

        readonly List<Vector2> _offsets = new List<Vector2>();
        ShipAgent _ship;
        ShipDurability _durability;

        public float MetalAmount => _metalAmount;
        public int PieceCount => _pieceCount;
        public float TotalMetal => _metalAmount * _pieceCount;

        // Subscribed for the object's whole life, like the hull it listens to.
        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _durability = GetComponent<ShipDurability>();
            _durability.DestroyedBy += OnDestroyedBy;
        }

        void OnDestroy()
        {
            _durability.DestroyedBy -= OnDestroyedBy;
        }

        void OnDestroyedBy(ShipAgent ship, GameText cause)
        {
            ZoneView zone = _ship.Zone;
            if (zone == null || _ship.IsDocked)
            {
                return;
            }

            Vector3 center = zone.transform.InverseTransformPoint(_ship.WorldPosition);
            float heading = _ship.HeadingDegrees;
            WreckScatter.Offsets(_pieceCount, _scatterRadiusUnits, heading, _offsets);
            for (int i = 0; i < _offsets.Count; i++)
            {
                Vector3 position = new Vector3(center.x + _offsets[i].x, 0f, center.z + _offsets[i].y);
                // Each piece turns its salvage slots differently, so the field does not look stamped.
                zone.AddDeposit(ResourceDeposit.Wreck(position, _metalAmount, heading + i * 47f, _pieceRadiusUnits));
            }
        }
    }
}
