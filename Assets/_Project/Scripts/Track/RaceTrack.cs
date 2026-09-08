// RaceTrack.cs
// Holds the ordered list of checkpoints that defines the track. Used by:
//  - RaceManager / LapTracker, to validate laps (must hit checkpoints in order)
//  - AIDriver, as the path to follow (same waypoints, no separate AI path needed)
// Keeping one source of truth avoids building two systems that can drift apart.

using System.Collections.Generic;
using UnityEngine;

namespace RacingPrototype
{
    public class RaceTrack : MonoBehaviour
    {
        [Tooltip("Checkpoints in driving order. Index 0 must be the start/finish line.")]
        [SerializeField] private List<Checkpoint> checkpoints = new List<Checkpoint>();

        [Tooltip("Total laps required to finish the race.")]
        [SerializeField] private int totalLaps = 3;

        public int TotalLaps => totalLaps;
        public int CheckpointCount => checkpoints.Count;

        public static RaceTrack Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] != null)
                    checkpoints[i].Index = i;
            }
        }

        public Checkpoint GetCheckpoint(int index)
        {
            if (checkpoints.Count == 0) return null;
            index = ((index % checkpoints.Count) + checkpoints.Count) % checkpoints.Count;
            return checkpoints[index];
        }

        public Vector3 GetCheckpointPosition(int index) => GetCheckpoint(index).transform.position;

        public int StartFinishIndex => 0;

#if UNITY_EDITOR
        // Editor-only convenience for the track builder tool to populate this list.
        public void Editor_SetCheckpoints(List<Checkpoint> cps, int laps)
        {
            checkpoints = cps;
            totalLaps = laps;
        }
#endif
    }
}
