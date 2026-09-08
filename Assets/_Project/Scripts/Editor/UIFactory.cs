// UIFactory.cs
// EDITOR-ONLY. Builds a minimal uGUI HUD: countdown, lap counter, position,
// speed, timer, and a finish panel with a restart button. Uses Unity's
// legacy Text component (not TextMeshPro) so the project has zero extra
// package dependencies out of the box.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RacingPrototype.EditorTools
{
    internal static class UIFactory
    {
        public static Transform BuildHUD(Transform parent, RaceManager raceManager, GameObject playerCar)
        {
            GameObject canvasGO = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(parent);
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // portrait-friendly default for eventual Android target
            scaler.matchWidthOrHeight = 0.5f;

            Text countdownText = CreateText(canvasGO.transform, "CountdownText", "3",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f), anchoredPos: Vector2.zero, size: new Vector2(400, 200), fontSize: 120,
                alignment: TextAnchor.MiddleCenter);

            Text lapText = CreateText(canvasGO.transform, "LapText", "LAP 1/3",
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), anchoredPos: new Vector2(30, -30), size: new Vector2(300, 60), fontSize: 40,
                alignment: TextAnchor.UpperLeft);

            Text positionText = CreateText(canvasGO.transform, "PositionText", "1/2",
                anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f), anchoredPos: new Vector2(-30, -30), size: new Vector2(200, 60), fontSize: 40,
                alignment: TextAnchor.UpperRight);

            Text timerText = CreateText(canvasGO.transform, "TimerText", "00:00.000",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f), anchoredPos: new Vector2(0, -30), size: new Vector2(300, 60), fontSize: 36,
                alignment: TextAnchor.UpperCenter);

            Text speedText = CreateText(canvasGO.transform, "SpeedText", "0 KPH",
                anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f),
                pivot: new Vector2(0.5f, 0f), anchoredPos: new Vector2(0, 720), size: new Vector2(300, 70), fontSize: 44,
                alignment: TextAnchor.LowerCenter);

            GameObject finishPanel = BuildFinishPanel(canvasGO.transform, out Text finishSummaryText);

            GameObject hudGO = new GameObject("RaceHUD");
            hudGO.transform.SetParent(parent);
            RaceHUD hud = hudGO.AddComponent<RaceHUD>();
            RestartHandler restartHandler = hudGO.AddComponent<RestartHandler>();

            var so = new SerializedObject(hud);
            so.FindProperty("raceManager").objectReferenceValue = raceManager;
            so.FindProperty("playerLapTracker").objectReferenceValue = playerCar.GetComponent<LapTracker>();
            so.FindProperty("playerCarController").objectReferenceValue = playerCar.GetComponent<CarController>();
            so.FindProperty("countdownText").objectReferenceValue = countdownText;
            so.FindProperty("lapText").objectReferenceValue = lapText;
            so.FindProperty("positionText").objectReferenceValue = positionText;
            so.FindProperty("speedText").objectReferenceValue = speedText;
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("finishPanel").objectReferenceValue = finishPanel;
            so.FindProperty("finishSummaryText").objectReferenceValue = finishSummaryText;
            so.ApplyModifiedPropertiesWithoutUndo();

            WireRestartButton(finishPanel.transform, restartHandler);

            return canvasGO.transform;
        }

        private static void WireRestartButton(Transform finishPanelTransform, RestartHandler restartHandler)
        {
            Button button = finishPanelTransform.Find("RestartButton")?.GetComponent<Button>();
            if (button == null) return;

            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, restartHandler.Restart);
        }

        private static GameObject BuildFinishPanel(Transform canvasParent, out Text summaryText)
        {
            GameObject panel = new GameObject("FinishPanel", typeof(Image));
            panel.transform.SetParent(canvasParent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            summaryText = CreateText(panel.transform, "FinishSummaryText", "FINISHED - P1\n00:00.000",
                anchorMin: new Vector2(0.5f, 0.6f), anchorMax: new Vector2(0.5f, 0.6f),
                pivot: new Vector2(0.5f, 0.5f), anchoredPos: Vector2.zero, size: new Vector2(700, 220), fontSize: 60,
                alignment: TextAnchor.MiddleCenter);

            GameObject buttonGO = new GameObject("RestartButton", typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(panel.transform);
            RectTransform btnRt = buttonGO.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.35f);
            btnRt.anchorMax = new Vector2(0.5f, 0.35f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = Vector2.zero;
            btnRt.sizeDelta = new Vector2(320, 100);
            buttonGO.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);

            CreateText(buttonGO.transform, "Label", "RESTART",
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f), anchoredPos: Vector2.zero, size: Vector2.zero, fontSize: 40,
                alignment: TextAnchor.MiddleCenter);

            return panel;
        }

        private static Text CreateText(Transform parent, string name, string content,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size,
            int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            if (size != Vector2.zero) rt.sizeDelta = size;
            else { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }

            Text text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }
    }
}
#endif
