// AIDriver.cs
// Deliberately simple: steer toward the next checkpoint, throttle based on
// how sharp the upcoming turn is, brake if a sharp turn is close. No navmesh,
// no pathfinding - the track's checkpoint list IS the path, so there's only
// one system to maintain (see RaceTrack.cs comments).
//
// Implements IDriveInput, so it plugs into CarController exactly like PlayerInput.

using UnityEngine;

namespace RacingPrototype
{
    [RequireComponent(typeof(CarController))]
    public class AIDriver : MonoBehaviour, IDriveInput
    {
        [Header("Steering")]
        [SerializeField] private float steerSharpness = 3f;

        [Header("Speed Control")]
        [SerializeField] [Range(0f, 1f)] private float maxThrottle = 0.85f;
        [SerializeField] private float sharpTurnAngle = 45f;     // degrees to next waypoint direction change
        [SerializeField] private float lookAheadForBrakingDist = 12f;
        [SerializeField] [Range(0f, 1f)] private float turnBrakeStrength = 0.5f;

        [Header("Rubber-banding (keeps race close, optional)")]
        [SerializeField] private bool rubberBandingEnabled = true;
        [SerializeField] [Range(0f, 0.3f)] private float maxThrottleBoost = 0.1f;

        private RaceTrack track;
        private LapTracker lapTracker;
        private int targetCheckpointIndex = 1;

        private void Awake()
        {
            lapTracker = GetComponent<LapTracker>();
        }

        private void Start()
        {
            track = RaceTrack.Instance;
        }

        public DriveInputState GetInput()
        {
            var state = new DriveInputState();
            if (track == null || track.CheckpointCount == 0) return state;

            // Stay in sync with whatever checkpoint LapTracker actually expects next -
            // this keeps AI steering and lap validation from ever disagreeing.
            if (lapTracker != null) targetCheckpointIndex = lapTracker.NextCheckpointIndex;

            Vector3 targetPos = track.GetCheckpointPosition(targetCheckpointIndex);
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            float distanceToTarget = toTarget.magnitude;
            Vector3 localTarget = transform.InverseTransformDirection(toTarget.normalized);
            float steerAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

            float rawSteer = (steerAngle / 45f) * steerSharpness;
            state.Steer = Mathf.Clamp(rawSteer, -1f, 1f);

            // Look at the checkpoint after next to anticipate corners.
            Vector3 nextNextPos = track.GetCheckpointPosition(targetCheckpointIndex + 1);
            float upcomingTurnAngle = Vector3.Angle(toTarget, nextNextPos - targetPos);

            float throttle = maxThrottle;
            if (rubberBandingEnabled)
            {
                throttle += GetRubberBandBoost();
            }

            if (upcomingTurnAngle > sharpTurnAngle && distanceToTarget < lookAheadForBrakingDist)
            {
                state.Brake = turnBrakeStrength;
                throttle *= 0.5f;
            }

            state.Throttle = Mathf.Clamp01(throttle);
            state.Reverse = false;
            state.Handbrake = false;

            return state;
        }

        private float GetRubberBandBoost()
        {
            var player = RaceManager.Instance != null ? RaceManager.Instance.PlayerLapTracker : null;
            if (player == null || lapTracker == null) return 0f;

            float diff = player.RaceProgress - lapTracker.RaceProgress;
            // Behind the player -> small throttle boost, capped, so AI stays competitive
            // without guaranteeing a rubber-banded win.
            float t = Mathf.Clamp01(diff / 10f);
            return t * maxThrottleBoost;
        }
    }
}
