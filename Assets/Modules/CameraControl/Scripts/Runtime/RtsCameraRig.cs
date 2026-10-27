using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFleet.CameraControl
{
    /// <summary>
    /// Isometric RTS camera (GDD: View, Controls). The camera orbits a pivot on a horizontal plane
    /// that is limited to a circular area.
    /// WASD / arrow keys: pan (Shift = faster), Q / E: rotate, middle mouse drag: rotate and tilt,
    /// mouse wheel: zoom towards the cursor, Home: return to the center of the area.
    /// Runs on unscaled time so the time-acceleration feature does not speed up the camera.
    /// </summary>
    public sealed class RtsCameraRig : MonoBehaviour
    {
        const float NotchesPerPlatformScrollUnit = 1f / 120f;
        const float PlatformScrollThreshold = 10f;

        [SerializeField] Camera _camera;

        [Header("Bounds")]
        [Tooltip("Center of the circular area the pivot may move in. Its height defines the pivot plane.")]
        [SerializeField] Vector3 _boundsCenter = Vector3.zero;
        [SerializeField, Min(1f)] float _boundsRadius = 500f;

        [Header("Zoom (Unity units)")]
        [SerializeField, Min(0.5f)] float _minimumDistance = 3f;
        [SerializeField, Min(1f)] float _maximumDistance = 1200f;
        [SerializeField, Min(0.5f)] float _startDistance = 120f;
        [Tooltip("Fraction of the current distance per mouse wheel notch.")]
        [SerializeField, Range(0.02f, 0.5f)] float _zoomStep = 0.15f;
        [SerializeField, Min(0f)] float _zoomSmoothTimeSeconds = 0.12f;

        [Header("Rotation (degrees)")]
        [SerializeField] float _startYaw = 45f;
        [SerializeField, Range(5f, 89f)] float _startPitch = 50f;
        [SerializeField, Range(5f, 89f)] float _minimumPitch = 15f;
        [SerializeField, Range(5f, 89f)] float _maximumPitch = 85f;
        [SerializeField] float _mouseRotateDegreesPerPixel = 0.25f;
        [SerializeField] float _keyRotateDegreesPerSecond = 90f;
        [SerializeField, Min(0f)] float _rotateSmoothTimeSeconds = 0.08f;

        [Header("Pan")]
        [Tooltip("Pan speed as a multiple of the camera distance per second.")]
        [SerializeField] float _panSpeed = 1f;
        [SerializeField] float _fastPanMultiplier = 3f;
        [SerializeField, Min(0f)] float _panSmoothTimeSeconds = 0.1f;

        Vector3 _pivot;
        Vector3 _targetPivot;
        Vector3 _pivotVelocity;
        float _yaw;
        float _targetYaw;
        float _yawVelocity;
        float _pitch;
        float _targetPitch;
        float _pitchVelocity;
        float _distance;
        float _targetDistance;
        float _distanceVelocity;

        public Camera Camera => _camera;
        public Vector3 Pivot => _pivot;
        public float Distance => _distance;

        void Awake()
        {
            if (_camera == null)
            {
                Debug.LogError("RtsCameraRig needs a camera reference.", this);
                enabled = false;
                return;
            }

            _pivot = _targetPivot = ClampPivot(_boundsCenter);
            _yaw = _targetYaw = _startYaw;
            _pitch = _targetPitch = Mathf.Clamp(_startPitch, _minimumPitch, _maximumPitch);
            _distance = _targetDistance = Mathf.Clamp(_startDistance, _minimumDistance, _maximumDistance);
            ApplyTransform();
        }

        void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            HandleZoom();
            HandleRotation(deltaTime);
            HandlePan(deltaTime);

            if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame)
            {
                FocusOn(_boundsCenter);
            }

            _pivot = Vector3.SmoothDamp(_pivot, _targetPivot, ref _pivotVelocity, _panSmoothTimeSeconds, Mathf.Infinity, deltaTime);
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, _rotateSmoothTimeSeconds, Mathf.Infinity, deltaTime);
            _pitch = Mathf.SmoothDamp(_pitch, _targetPitch, ref _pitchVelocity, _rotateSmoothTimeSeconds, Mathf.Infinity, deltaTime);
            _distance = Mathf.SmoothDamp(_distance, _targetDistance, ref _distanceVelocity, _zoomSmoothTimeSeconds, Mathf.Infinity, deltaTime);

            ApplyTransform();
        }

        /// <summary>Changes the area the pivot may move in, e.g. when switching between map views.</summary>
        public void SetBounds(Vector3 center, float radius)
        {
            _boundsCenter = center;
            _boundsRadius = Mathf.Max(1f, radius);
            _targetPivot = ClampPivot(_targetPivot);
        }

        /// <summary>Moves the camera smoothly to a point, optionally with a new distance.</summary>
        public void FocusOn(Vector3 worldPoint, float? distance = null)
        {
            _targetPivot = ClampPivot(worldPoint);
            if (distance.HasValue)
            {
                _targetDistance = Mathf.Clamp(distance.Value, _minimumDistance, _maximumDistance);
            }
        }

        /// <summary>Moves the camera to a point instantly, e.g. when switching to another zone.</summary>
        public void SnapTo(Vector3 worldPoint, float distance)
        {
            _pivot = _targetPivot = ClampPivot(worldPoint);
            _distance = _targetDistance = Mathf.Clamp(distance, _minimumDistance, _maximumDistance);
            _pivotVelocity = Vector3.zero;
            _distanceVelocity = 0f;
            ApplyTransform();
        }

        void HandleZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            // Depending on platform and settings the Input System reports 120 or 1 per wheel notch.
            float notches = Mathf.Abs(scroll) > PlatformScrollThreshold ? scroll * NotchesPerPlatformScrollUnit : scroll;
            float previousDistance = _targetDistance;
            _targetDistance = Mathf.Clamp(previousDistance * Mathf.Pow(1f - _zoomStep, notches), _minimumDistance, _maximumDistance);

            // When zooming in, the pivot moves towards the point under the cursor.
            Plane pivotPlane = new Plane(Vector3.up, _boundsCenter);
            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            if (_targetDistance < previousDistance && pivotPlane.Raycast(ray, out float enter))
            {
                float t = 1f - _targetDistance / previousDistance;
                _targetPivot = ClampPivot(Vector3.Lerp(_targetPivot, ray.GetPoint(enter), t));
            }
        }

        void HandleRotation(float deltaTime)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.middleButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _targetYaw += delta.x * _mouseRotateDegreesPerPixel;
                _targetPitch -= delta.y * _mouseRotateDegreesPerPixel;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed)
                {
                    _targetYaw += _keyRotateDegreesPerSecond * deltaTime;
                }
                if (keyboard.eKey.isPressed)
                {
                    _targetYaw -= _keyRotateDegreesPerSecond * deltaTime;
                }
            }

            _targetPitch = Mathf.Clamp(_targetPitch, _minimumPitch, _maximumPitch);
        }

        void HandlePan(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }
            if (input == Vector2.zero)
            {
                return;
            }

            float speed = _panSpeed * _targetDistance;
            if (keyboard.leftShiftKey.isPressed)
            {
                speed *= _fastPanMultiplier;
            }

            Quaternion heading = Quaternion.Euler(0f, _targetYaw, 0f);
            Vector3 move = heading * new Vector3(input.x, 0f, input.y).normalized;
            _targetPivot = ClampPivot(_targetPivot + move * speed * deltaTime);
        }

        Vector3 ClampPivot(Vector3 point)
        {
            Vector3 offset = point - _boundsCenter;
            offset.y = 0f;
            return _boundsCenter + Vector3.ClampMagnitude(offset, _boundsRadius);
        }

        void ApplyTransform()
        {
            transform.position = _pivot;

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(_pivot - rotation * Vector3.forward * _distance, rotation);

            // Clip planes follow the distance so depth precision is sufficient at every zoom level.
            _camera.nearClipPlane = Mathf.Max(0.05f, _distance * 0.01f);
            _camera.farClipPlane = _distance + _boundsRadius * 2f + 500f;
        }

        void OnValidate()
        {
            _maximumDistance = Mathf.Max(_maximumDistance, _minimumDistance);
            _maximumPitch = Mathf.Max(_maximumPitch, _minimumPitch);
        }
    }
}
