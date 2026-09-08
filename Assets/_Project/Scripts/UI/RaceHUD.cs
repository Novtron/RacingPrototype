// RaceHUD.cs
// Pure display: reads RaceManager / LapTracker / CarController and writes
// to uGUI Text elements. Contains no race rules itself, so UI can be
// restyled or replaced without touching gameplay code.

using UnityEngine;
using UnityEngine.UI;

namespace RacingPrototype
{
    public class RaceHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private LapTracker playerLapTracker;
        [SerializeField] private CarController playerCarController;

        [Header("HUD Elements")]
        [SerializeField] private Text countdownText;
        [SerializeField] private Text lapText;
        [SerializeField] private Text positionText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text timerText;

        [Header("Finish Screen")]
        [SerializeField] private GameObject finishPanel;
        [SerializeField] private Text finishSummaryText;

        private void Start()
        {
            if (finishPanel != null) finishPanel.SetActive(false);
            if (raceManager != null) raceManager.OnPlayerFinished += ShowFinishScreen;
        }

        private void OnDestroy()
        {
            if (raceManager != null) raceManager.OnPlayerFinished -= ShowFinishScreen;
        }

        private void Update()
        {
            if (raceManager == null) return;

            UpdateCountdown();
            UpdateLap();
            UpdatePosition();
            UpdateSpeed();
            UpdateTimer();
        }

        private void UpdateCountdown()
        {
            if (countdownText == null) return;

            if (raceManager.State == RaceState.Countdown)
            {
                countdownText.gameObject.SetActive(true);
                int secondsLeft = Mathf.CeilToInt(raceManager.CountdownRemaining);
                countdownText.text = secondsLeft > 0 ? secondsLeft.ToString() : "GO!";
            }
            else
            {
                countdownText.gameObject.SetActive(false);
            }
        }

        private void UpdateLap()
        {
            if (lapText == null || playerLapTracker == null) return;
            int totalLaps = RaceTrack.Instance != null ? RaceTrack.Instance.TotalLaps : 0;
            lapText.text = $"LAP {Mathf.Min(playerLapTracker.CurrentLap, totalLaps)}/{totalLaps}";
        }

        private void UpdatePosition()
        {
            if (positionText == null || playerLapTracker == null) return;
            int pos = raceManager.GetPositionOf(playerLapTracker);
            int total = raceManager.StandingsOrder.Count;
            positionText.text = $"{pos}/{total}";
        }

        private void UpdateSpeed()
        {
            if (speedText == null || playerCarController == null) return;
            int kph = Mathf.RoundToInt(Mathf.Abs(playerCarController.CurrentSpeedKph));
            speedText.text = $"{kph} KPH";
        }

        private void UpdateTimer()
        {
            if (timerText == null) return;
            timerText.text = FormatTime(raceManager.RaceTime);
        }

        private void ShowFinishScreen()
        {
            if (finishPanel == null) return;
            finishPanel.SetActive(true);

            if (finishSummaryText != null)
            {
                int pos = raceManager.GetPositionOf(playerLapTracker);
                finishSummaryText.text = $"FINISHED - P{pos}\nTIME: {FormatTime(playerLapTracker.FinishTime)}";
            }
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int millis = Mathf.FloorToInt((seconds * 1000f) % 1000f);
            return $"{minutes:00}:{secs:00}.{millis:000}";
        }
    }
}
