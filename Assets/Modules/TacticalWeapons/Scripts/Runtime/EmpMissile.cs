using System;
using UnityEngine;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// An EMP missile in flight: it flies straight to a fixed point of its zone and goes off there. It
    /// cannot be shot down. It lives in the zone, so it is gone with the zone when the fleet leaves.
    /// </summary>
    public sealed class EmpMissile : MonoBehaviour
    {
        Vector3 _targetLocalPoint;
        float _speedUnitsPerSecond;
        Action<EmpMissile> _arrived;

        /// <summary>The point it flies to, in its zone's local space.</summary>
        public Vector3 TargetLocalPoint => _targetLocalPoint;

        public void Launch(Vector3 targetLocalPoint, float speedUnitsPerSecond, Action<EmpMissile> arrived)
        {
            _targetLocalPoint = targetLocalPoint;
            _speedUnitsPerSecond = speedUnitsPerSecond;
            _arrived = arrived;
            FaceTarget();
        }

        void Update()
        {
            if (_arrived == null)
            {
                return;
            }

            Vector3 position = transform.localPosition;
            Vector3 next = Vector3.MoveTowards(position, _targetLocalPoint, _speedUnitsPerSecond * Time.deltaTime);
            transform.localPosition = next;
            if ((next - _targetLocalPoint).sqrMagnitude > 1e-6f)
            {
                FaceTarget();
                return;
            }

            Action<EmpMissile> arrived = _arrived;
            _arrived = null;
            arrived(this);
            Destroy(gameObject);
        }

        void FaceTarget()
        {
            Vector3 direction = _targetLocalPoint - transform.localPosition;
            if (direction.sqrMagnitude > 1e-6f)
            {
                transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
    }
}
