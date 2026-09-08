// TouchControlsFactory.cs
// EDITOR-ONLY. Builds the on-screen touch control buttons (steer left/right,
// throttle, brake, reverse, handbrake) inside the HUD canvas, and assigns
// them as SerializeField references on TouchInput. Assigning object
// references this way (rather than subscribing to C# events directly from
// this editor script) is what makes the wiring actually survive into Play
// Mode and real builds - see the comment at the top of TouchInput.cs.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RacingPrototype.EditorTools
{
    internal static class TouchControlsFactory
    {
        private static readonly Color SteerColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color ThrottleColor = new Color(0.25f, 0.8f, 0.3f, 0.55f);
        private static readonly Color BrakeColor = new Color(0.85f, 0.2f, 0.15f, 0.55f);
        private static readonly Color ReverseColor = new Color(0.9f, 0.55f, 0.1f, 0.55f);
        private static readonly Color HandbrakeColor = new Color(0.7f, 0.7f, 0.1f, 0.55f);

        public static void BuildTouchControls(Transform canvasTransform, GameObject playerCar)
        {
            TouchInput touchInput = playerCar.GetComponent<TouchInput>();
            if (touchInput == null)
            {
                Debug.LogWarning("TouchControlsFactory: player car has no TouchInput component; skipping on-screen controls.");
                return;
            }

            GameObject controlsRoot = new GameObject("TouchControls", typeof(RectTransform));
            controlsRoot.transform.SetParent(canvasTransform, false);
            RectTransform rootRt = controlsRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // ---- Steering: bottom-left pair ----
            HoldButton steerLeft = CreateRoundButton(controlsRoot.transform, "Btn_SteerLeft", "<",
                anchor: new Vector2(0f, 0f), anchoredPos: new Vector2(110, 160), diameter: 170, SteerColor);
            HoldButton steerRight = CreateRoundButton(controlsRoot.transform, "Btn_SteerRight", ">",
                anchor: new Vector2(0f, 0f), anchoredPos: new Vector2(300, 160), diameter: 170, SteerColor);

            // ---- Throttle / Brake / Reverse: bottom-right stack ----
            HoldButton throttle = CreateRoundButton(controlsRoot.transform, "Btn_Throttle", "GAS",
                anchor: new Vector2(1f, 0f), anchoredPos: new Vector2(-160, 380), diameter: 190, ThrottleColor);
            HoldButton brake = CreateRoundButton(controlsRoot.transform, "Btn_Brake", "BRAKE",
                anchor: new Vector2(1f, 0f), anchoredPos: new Vector2(-160, 175), diameter: 170, BrakeColor);
            HoldButton reverse = CreateRoundButton(controlsRoot.transform, "Btn_Reverse", "REV",
                anchor: new Vector2(1f, 0f), anchoredPos: new Vector2(-370, 160), diameter: 140, ReverseColor);

            // ---- Handbrake: small button above the throttle stack ----
            HoldButton handbrake = CreateRoundButton(controlsRoot.transform, "Btn_Handbrake", "HB",
                anchor: new Vector2(1f, 0f), anchoredPos: new Vector2(-160, 620), diameter: 130, HandbrakeColor);

            var so = new SerializedObject(touchInput);
            so.FindProperty("steerLeftButton").objectReferenceValue = steerLeft;
            so.FindProperty("steerRightButton").objectReferenceValue = steerRight;
            so.FindProperty("throttleButton").objectReferenceValue = throttle;
            so.FindProperty("brakeButton").objectReferenceValue = brake;
            so.FindProperty("reverseButton").objectReferenceValue = reverse;
            so.FindProperty("handbrakeButton").objectReferenceValue = handbrake;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HoldButton CreateRoundButton(Transform parent, string name, string label,
            Vector2 anchor, Vector2 anchoredPos, float diameter, Color color)
        {
            GameObject go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(diameter, diameter);

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = CreateCircleSprite();
            image.type = Image.Type.Simple;

            HoldButton holdButton = go.AddComponent<HoldButton>();

            GameObject labelGO = new GameObject("Label", typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            RectTransform labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            Text text = labelGO.GetComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(diameter * 0.22f);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false; // let clicks pass through to the button behind it

            return holdButton;
        }

        // Generates a simple filled-circle sprite at runtime so buttons read as
        // round touch targets without needing any imported texture asset.
        private static Sprite cachedCircleSprite;

        private static Sprite CreateCircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    texture.SetPixel(x, y, dist <= radius ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }
            texture.Apply();
            texture.name = "TouchButtonCircle";

            cachedCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedCircleSprite;
        }
    }
}
#endif
