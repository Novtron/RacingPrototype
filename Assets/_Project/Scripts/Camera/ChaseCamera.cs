// ChaseCamera.cs
// Simple spring-follow third-person camera. No cinemachine dependency -
// keeps the project lightweight for Android and easy to understand.
// Looks at a target offset slightly above the car so the car doesn't
// fill the whole frame.

using UnityEngine;

namespace RacingPrototype
{
    public class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3.5f, -7f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float rotationSmoothSpeed = 6f;

        private Vector3 velocity;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, positionSmoothTime);

            Vector3 lookAtPoint = target.position + target.TransformDirection(lookAtOffset);
            Quaternion desiredRotation = Quaternion.LookRotation(lookAtPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }
}
