using System.Collections.Generic;
using LastFleet.Shared.Blockout;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// Marks stunned ships of both sides in the zone: a flickering ring in the EMP's color around the
    /// ship, which pulls in towards the hull as the stun wears off. Looks only; the rings are pooled
    /// here, so no ship prefab needs a part of its own.
    /// </summary>
    public sealed class StunMarkerView : MonoBehaviour
    {
        const int RingSegments = 40;

        [SerializeField] ShipRegistry[] _registries = new ShipRegistry[0];
        [SerializeField] Material _ringMaterial;
        [Tooltip("Ring radius as a share of a capital ship's obstacle radius.")]
        [SerializeField, Min(1f)] float _capitalRadiusFactor = 1.5f;
        [Tooltip("Ring radius around small ships, which have no obstacle radius.")]
        [SerializeField, Min(0.1f)] float _smallShipRadiusUnits = 3f;
        [Tooltip("The ring starts this much wider and closes in on the ship as the stun ends.")]
        [SerializeField, Min(1f)] float _startWidening = 1.6f;
        [SerializeField, Min(0.01f)] float _lineWidthUnits = 0.35f;
        [SerializeField, Min(0.1f)] float _flickersPerSecond = 6f;

        readonly Dictionary<ShipAgent, Marker> _markers = new Dictionary<ShipAgent, Marker>();
        readonly List<ShipAgent> _gone = new List<ShipAgent>();
        readonly Stack<LineRenderer> _spareRings = new Stack<LineRenderer>();

        sealed class Marker
        {
            public LineRenderer Ring;
            public float StartSeconds;
        }

        void Awake()
        {
            if (_ringMaterial == null || _registries.Length == 0)
            {
                Debug.LogError("StunMarkerView needs the ship registries and a ring material.", this);
                enabled = false;
            }
        }

        void LateUpdate()
        {
            foreach (ShipRegistry registry in _registries)
            {
                foreach (ShipAgent ship in registry.Ships)
                {
                    if (ship.IsStunned && !ship.IsDocked && ship.Zone != null)
                    {
                        ShowMarker(ship);
                    }
                }
            }
            HideFinishedMarkers();
        }

        void ShowMarker(ShipAgent ship)
        {
            if (!_markers.TryGetValue(ship, out Marker marker))
            {
                marker = new Marker { Ring = TakeRing(), StartSeconds = ship.StunSecondsLeft };
                _markers.Add(ship, marker);
            }
            // A second EMP can stun the ship for longer than the first.
            marker.StartSeconds = Mathf.Max(marker.StartSeconds, ship.StunSecondsLeft);

            float left = marker.StartSeconds > 0f ? ship.StunSecondsLeft / marker.StartSeconds : 0f;
            float baseRadius = ship.IsCapital ? ship.ObstacleRadiusUnits * _capitalRadiusFactor : _smallShipRadiusUnits;
            float flicker = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * _flickersPerSecond * Mathf.PI));

            Transform ringTransform = marker.Ring.transform;
            ringTransform.position = ship.WorldPosition;
            ringTransform.rotation = ship.Zone.transform.rotation;
            ringTransform.localScale = Vector3.one * (baseRadius * Mathf.Lerp(1f, _startWidening, left));
            marker.Ring.widthMultiplier = _lineWidthUnits * (ship.IsCapital ? 2f : 1f) * flicker;
            marker.Ring.enabled = true;
        }

        void HideFinishedMarkers()
        {
            _gone.Clear();
            foreach (KeyValuePair<ShipAgent, Marker> entry in _markers)
            {
                ShipAgent ship = entry.Key;
                if (ship == null || !ship.IsStunned || ship.IsDocked || ship.Zone == null)
                {
                    _gone.Add(ship);
                }
            }
            foreach (ShipAgent ship in _gone)
            {
                LineRenderer ring = _markers[ship].Ring;
                ring.enabled = false;
                _spareRings.Push(ring);
                _markers.Remove(ship);
            }
        }

        LineRenderer TakeRing()
        {
            if (_spareRings.Count > 0)
            {
                return _spareRings.Pop();
            }
            LineRenderer ring = BlockoutPrimitives.CreateLine(transform, _ringMaterial, "Stun Marker", _lineWidthUnits);
            BlockoutPrimitives.SetCircle(ring, Vector3.zero, 1f, RingSegments);
            return ring;
        }
    }
}
