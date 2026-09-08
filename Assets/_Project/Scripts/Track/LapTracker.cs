// LapTracker.cs
// Attach to every car (player and AI). Requires checkpoints to be hit in
// strict order to prevent shortcut/reverse-driving exploits. Exposes a
// single "race progress" number that RaceManager uses to compute standings
// without needing to know anything about track shape.
//
// Cars are assumed to start sitting at checkpoint 0 (the start/finish line),
// so the first checkpoint they need to reach is index 1. A lap is complete
// the moment they hit checkpoint 0 again after visiting every other checkpoint.

using System;
using UnityEngine;

namespace RacingPrototype
{
    public class LapTracker : MonoBehaviour
    {
        public event Action<int> OnLapCompleted;      // passes new lap count
        public event Action OnRaceFinished;

        [SerializeField] private string driverName = "Car";
        public string DriverName => driverName;

        public int CurrentLap { get; private set; } = 1;
        public int NextCheckpointIndex { get; private set; } = 1;
        public bool Finished { get; private set; }
        public float FinishTime { get; private set; } = -1f;

        private RaceTrack track;

        // Progress = (laps completed so far * checkpoints) + checkpoints passed this lap.
        // Monotonically increasing along the intended driving direction; used directly
        // for position sorting (highest progress = 1st place).
        public float RaceProgress
        {
            get
            {
                if (track == null || track.CheckpointCount == 0) return 0f;
                int checkpointsPassedThisLap = NextCheckpointIndex == 0
                    ? track.CheckpointCount
                    : NextCheckpointIndex;
                return (CurrentLap - 1) * track.CheckpointCount + checkpointsPassedThisLap;
            }
        }

        private void Start()
        {
            track = RaceTrack.Instance;
        }

        public void NotifyCheckpointHit(int checkpointIndex)
        {
            if (Finished || track == null) return;
            if (checkpointIndex != NextCheckpointIndex) return; // wrong-order hit, ignore

            bool completingLap = checkpointIndex == track.StartFinishIndex;
            NextCheckpointIndex = (NextCheckpointIndex + 1) % track.CheckpointCount;

            if (completingLap)
            {
                CurrentLap++;
                OnLapCompleted?.Invoke(CurrentLap);

                if (CurrentLap > track.TotalLaps)
                {
                    Finished = true;
                    FinishTime = Time.timeSinceLevelLoad;
                    OnRaceFinished?.Invoke();
                }
            }
        }
    }
}
