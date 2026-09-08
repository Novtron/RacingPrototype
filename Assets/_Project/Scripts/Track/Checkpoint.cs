// Checkpoint.cs
// A trigger volume placed along the track. Index 0 is treated as the
// start/finish line by convention (RaceTrack.StartFinishIndex).
// Fires an event any LapTracker on a car can subscribe to via OnTriggerEnter.

using UnityEngine;

namespace RacingPrototype
{
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        public int Index { get; set; }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var lapTracker = other.GetComponentInParent<LapTracker>();
            if (lapTracker != null)
            {
                lapTracker.NotifyCheckpointHit(Index);
            }
        }
    }
}
