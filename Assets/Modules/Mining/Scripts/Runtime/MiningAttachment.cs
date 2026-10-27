using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Presentation of mining: while extracting, the harvester's model glides onto the rock and clings
    /// to it like a remora, and debris sprays from the contact point. Only the model moves; the ship
    /// itself stays at its mining slot, so movement and selection are unaffected.
    /// </summary>
    [RequireComponent(typeof(HarvesterMining))]
    public sealed class MiningAttachment : MonoBehaviour
    {
        // The rock blockout's main lump fills about this share of the deposit radius.
        const float VisibleSurfaceFactor = 0.85f;

        [SerializeField] Transform _model;
        [SerializeField] ParticleSystem _debris;
        [SerializeField, Min(0.01f)] float _attachSeconds = 1.2f;
        [Tooltip("Distance from the rock surface to the model's origin, about half the hull height.")]
        [SerializeField, Min(0f)] float _hullClearanceUnits = 0.3f;

        HarvesterMining _mining;
        ShipPresentation _presentation;
        Vector3 _restLocalPosition;
        Quaternion _restLocalRotation;
        Vector3 _clampPosition;
        Quaternion _clampRotation;
        float _progress;
        ResourceKind _debrisResource;
        bool _hasDebrisColor;

        void Awake()
        {
            _mining = GetComponent<HarvesterMining>();
            TryGetComponent(out _presentation);
            if (_model == null || _debris == null)
            {
                Debug.LogError("MiningAttachment needs the model transform and a debris particle system.", this);
                enabled = false;
                return;
            }
            _restLocalPosition = _model.localPosition;
            _restLocalRotation = _model.localRotation;
        }

        void OnDisable()
        {
            // Docking deactivates the ship; it must not come back clamped to a rock.
            _progress = 0f;
            if (_model != null)
            {
                _model.SetLocalPositionAndRotation(_restLocalPosition, _restLocalRotation);
            }
        }

        void LateUpdate()
        {
            ResourceDeposit deposit = _mining.Deposit;
            bool clamped = _mining.State == MiningState.Extracting && deposit != null;
            _progress = Mathf.MoveTowards(_progress, clamped ? 1f : 0f, Time.deltaTime / _attachSeconds);

            if (clamped)
            {
                // Follow the shrinking rock; the last pose is kept so detaching glides back from it.
                float surfaceRadius = deposit.RadiusUnits * VisibleSurfaceFactor * Mathf.Pow(deposit.RemainingFraction, 1f / 3f);
                HullClampPose.Compute(_mining.DepositWorldPosition, surfaceRadius, transform.position, transform.forward,
                    _hullClearanceUnits, out _clampPosition, out _clampRotation);
                UpdateDebrisColor(deposit.Resource);
            }

            UpdateDebris(clamped && _progress >= 1f);
            // The trail would otherwise draw the glide onto the rock as a long streak.
            if (_presentation != null)
            {
                _presentation.SetTrailEmitting(_progress <= 0f);
            }
            if (_progress <= 0f)
            {
                _model.SetLocalPositionAndRotation(_restLocalPosition, _restLocalRotation);
                return;
            }

            float eased = Mathf.SmoothStep(0f, 1f, _progress);
            Vector3 restPosition = transform.TransformPoint(_restLocalPosition);
            Quaternion restRotation = transform.rotation * _restLocalRotation;
            _model.SetPositionAndRotation(Vector3.Lerp(restPosition, _clampPosition, eased),
                Quaternion.Slerp(restRotation, _clampRotation, eased));
        }

        void UpdateDebris(bool emitting)
        {
            if (emitting && !_debris.isEmitting)
            {
                _debris.Play();
            }
            else if (!emitting && _debris.isEmitting)
            {
                _debris.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        void UpdateDebrisColor(ResourceKind resource)
        {
            if (_hasDebrisColor && resource == _debrisResource)
            {
                return;
            }

            ParticleSystem.MainModule main = _debris.main;
            main.startColor = DebrisColor(resource);
            _debrisResource = resource;
            _hasDebrisColor = true;
        }

        static Color DebrisColor(ResourceKind resource)
        {
            return resource switch
            {
                ResourceKind.Ore => new Color(0.55f, 0.42f, 0.3f),
                ResourceKind.Ice => new Color(0.85f, 0.95f, 1f),
                ResourceKind.Gas => new Color(1f, 0.6f, 0.25f),
                _ => Color.magenta
            };
        }
    }
}
