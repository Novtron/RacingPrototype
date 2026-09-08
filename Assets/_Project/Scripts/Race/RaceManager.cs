// RaceManager.cs
// Owns overall race state: countdown -> racing -> finished. Computes live
// positions by sorting every car's LapTracker.RaceProgress. UI and camera
// read from here; nothing about track shape or car physics lives in this file.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RacingPrototype
{
    public enum RaceState { Countdown, Racing, Finished }

    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Setup")]
        [SerializeField] private List<LapTracker> racers = new List<LapTracker>();
        [SerializeField] private LapTracker playerLapTracker;

        [Header("Countdown")]
        [SerializeField] private float countdownSeconds = 3f;

        public RaceState State { get; private set; } = RaceState.Countdown;
        public float CountdownRemaining { get; private set; }
        public float RaceTime { get; private set; }
        public LapTracker PlayerLapTracker => playerLapTracker;

        // Ordered best (index 0) to worst.
        public IReadOnlyList<LapTracker> StandingsOrder { get; private set; } = new List<LapTracker>();

        public event Action OnCountdownFinished;
        public event Action OnPlayerFinished;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StandingsOrder = new List<LapTracker>(racers);
            FreezeCarsForCountdown(true);
            StartCoroutine(CountdownRoutine());

            foreach (var racer in racers)
            {
                if (racer == null) continue;
                racer.OnRaceFinished += () => HandleRacerFinished(racer);
            }
        }

        private void Update()
        {
            if (State != RaceState.Racing) return;

            RaceTime += Time.deltaTime;
            RecomputeStandings();
        }

        private IEnumerator CountdownRoutine()
        {
            CountdownRemaining = countdownSeconds;
            while (CountdownRemaining > 0f)
            {
                yield return null;
                CountdownRemaining -= Time.deltaTime;
            }
            CountdownRemaining = 0f;

            FreezeCarsForCountdown(false);
            State = RaceState.Racing;
            OnCountdownFinished?.Invoke();
        }

        private void FreezeCarsForCountdown(bool freeze)
        {
            // Freezing the Rigidbody is sufficient on its own: it stops all physical
            // movement regardless of what input scripts compute. We deliberately do NOT
            // also disable PlayerInput/AIDriver here, since CarController calls
            // IDriveInput.GetInput() as a plain method call each FixedUpdate - disabling
            // the component would not stop that call and would just be misleading.
            foreach (var racer in racers)
            {
                if (racer == null) continue;
                var rb = racer.GetComponent<Rigidbody>();
                if (rb == null) continue;

                if (freeze)
                {
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints.None;
                }
            }
        }

        private void RecomputeStandings()
        {
            StandingsOrder = racers
                .Where(r => r != null)
                .OrderByDescending(r => r.RaceProgress)
                .ToList();
        }

        public int GetPositionOf(LapTracker racer)
        {
            var list = StandingsOrder;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == racer) return i + 1;
            }
            return list.Count;
        }

        private void HandleRacerFinished(LapTracker racer)
        {
            if (racer == playerLapTracker)
            {
                State = RaceState.Finished;
                OnPlayerFinished?.Invoke();
            }
        }
    }
}
