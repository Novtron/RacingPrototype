// CarController.cs
// Arcade-style vehicle physics built on Unity's WheelCollider component.
// Deliberately NOT a realistic sim: torque/steer curves are tuned for
// responsive, forgiving handling. Works identically regardless of whether
// the input comes from on-screen touch controls (TouchInput), a keyboard
// (PlayerInput, kept for optional desktop testing), or the AI (AIDriver) -
// all just implement IDriveInput.
//
// Visuals are fully decoupled: this script never creates or assumes meshes.
// It only needs wheelVisualFL/FR/RL/RR transforms to spin/turn for feedback,
// and a bodyVisualRoot which can be swapped out later (primitives -> Tripo AI model)
// without touching this script at all.

using UnityEngine;

namespace RacingPrototype
{
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Wheel Colliders (physics)")]
        [SerializeField] private WheelCollider wheelColliderFL;
        [SerializeField] private WheelCollider wheelColliderFR;
        [SerializeField] private WheelCollider wheelColliderRL;
        [SerializeField] private WheelCollider wheelColliderRR;

        [Header("Wheel Visuals (optional, purely cosmetic)")]
        [SerializeField] private Transform wheelVisualFL;
        [SerializeField] private Transform wheelVisualFR;
        [SerializeField] private Transform wheelVisualRL;
        [SerializeField] private Transform wheelVisualRR;

        [Header("Arcade Tuning")]
        [SerializeField] private float maxMotorTorque = 1800f;
        [SerializeField] private float maxBrakeTorque = 4000f;
        [SerializeField] private float maxReverseTorque = 900f;
        [SerializeField] private float maxSteerAngle = 30f;
        [SerializeField] private AnimationCurve steerAngleByStableSpeed = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
        [SerializeField] private float topSpeedKph = 140f;
        [SerializeField] private float handbrakeRearBrakeTorque = 6000f;
        [SerializeField] private float handbrakeSidewaysGripMultiplier = 0.4f;
        [SerializeField] private float downforceAtTopSpeed = 40f;

        [Header("Stability")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);
        [SerializeField] private float slowSpeedThresholdForReverse = 1.5f; // km/h below which reverse can engage

        private Rigidbody rb;
        private IDriveInput input;
        private DriveInputState currentInput;

        private WheelFrictionCurve rearSidewaysFrictionDefault;
        private bool frictionCached;

        public float CurrentSpeedKph { get; private set; }
        public float CurrentSpeedMps { get; private set; }

        // Small public hook other systems (camera, UI) can read without coupling to input source.
        public Rigidbody Rigidbody => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass += centerOfMassOffset;

            input = GetComponent<IDriveInput>();
            if (input == null)
            {
                Debug.LogWarning($"{name}: CarController found no IDriveInput component. " +
                                  "Add TouchInput, PlayerInput, or AIDriver so the car can be controlled.");
            }

            // Snap wheel visuals to their correct pose immediately so they don't
            // flash at the car's origin for a frame before the first FixedUpdate.
            UpdateWheelVisuals();
        }

        private void FixedUpdate()
        {
            currentInput = input != null ? input.GetInput() : default;

            UpdateSpeedReadout();
            ApplySteering();
            ApplyMotorAndBrakes();
            ApplyHandbrake();
            ApplyDownforce();
            UpdateWheelVisuals();
        }

        private void UpdateSpeedReadout()
        {
            CurrentSpeedMps = Vector3.Dot(rb.linearVelocity, transform.forward);
            CurrentSpeedKph = CurrentSpeedMps * 3.6f;
        }

        private void ApplySteering()
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(CurrentSpeedKph) / topSpeedKph);
            float steerScale = steerAngleByStableSpeed.Evaluate(speed01);
            float angle = currentInput.Steer * maxSteerAngle * steerScale;

            wheelColliderFL.steerAngle = angle;
            wheelColliderFR.steerAngle = angle;
        }

        private void ApplyMotorAndBrakes()
        {
            float speedAbsKph = Mathf.Abs(CurrentSpeedKph);
            bool movingForward = CurrentSpeedMps > 0.3f;
            bool nearStopped = speedAbsKph < slowSpeedThresholdForReverse;

            float motor = 0f;
            float brake = 0f;

            if (currentInput.Reverse)
            {
                if (movingForward && !nearStopped)
                {
                    // Still rolling forward: reverse input means "brake" first.
                    brake = currentInput.Brake * maxBrakeTorque;
                }
                else
                {
                    // Stopped or already reversing: apply reverse motor torque.
                    motor = -currentInput.Brake * maxReverseTorque;
                }
            }
            else
            {
                float speedLimitScale = 1f - Mathf.Clamp01(speedAbsKph / topSpeedKph);
                motor = currentInput.Throttle * maxMotorTorque * speedLimitScale;
                brake = currentInput.Brake * maxBrakeTorque;
            }

            // Rear-wheel drive: simple, stable, and reads clearly as "arcade car."
            wheelColliderRL.motorTorque = motor;
            wheelColliderRR.motorTorque = motor;

            wheelColliderFL.brakeTorque = brake;
            wheelColliderFR.brakeTorque = brake;
            wheelColliderRL.brakeTorque = brake;
            wheelColliderRR.brakeTorque = brake;
        }

        private void ApplyHandbrake()
        {
            if (!frictionCached)
            {
                rearSidewaysFrictionDefault = wheelColliderRL.sidewaysFriction;
                frictionCached = true;
            }

            if (currentInput.Handbrake)
            {
                wheelColliderRL.brakeTorque = handbrakeRearBrakeTorque;
                wheelColliderRR.brakeTorque = handbrakeRearBrakeTorque;

                WheelFrictionCurve loose = rearSidewaysFrictionDefault;
                loose.stiffness = rearSidewaysFrictionDefault.stiffness * handbrakeSidewaysGripMultiplier;
                wheelColliderRL.sidewaysFriction = loose;
                wheelColliderRR.sidewaysFriction = loose;
            }
            else
            {
                wheelColliderRL.sidewaysFriction = rearSidewaysFrictionDefault;
                wheelColliderRR.sidewaysFriction = rearSidewaysFrictionDefault;
            }
        }

        private void ApplyDownforce()
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(CurrentSpeedKph) / topSpeedKph);
            rb.AddForce(-transform.up * (downforceAtTopSpeed * speed01), ForceMode.Force);
        }

        private void UpdateWheelVisuals()
        {
            UpdateSingleWheelVisual(wheelColliderFL, wheelVisualFL);
            UpdateSingleWheelVisual(wheelColliderFR, wheelVisualFR);
            UpdateSingleWheelVisual(wheelColliderRL, wheelVisualRL);
            UpdateSingleWheelVisual(wheelColliderRR, wheelVisualRR);
        }

        private static void UpdateSingleWheelVisual(WheelCollider col, Transform visual)
        {
            if (visual == null) return;
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            visual.SetPositionAndRotation(pos, rot);
        }
    }
}
