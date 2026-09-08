// TouchInput.cs
// On-screen touch control scheme for Android. Implements IDriveInput exactly
// like PlayerInput does, so CarController needs zero changes.
//
// IMPORTANT: the HoldButton references below are wired up here, in Awake, and
// NOT by an Editor script subscribing to C# events directly. C# event
// subscriptions (+=) made by editor tooling at scene-build time are pure
// runtime state - they are NOT part of what Unity serializes into the scene
// file, so they would silently vanish the moment Play Mode (or a real build)
// starts fresh. Serializable object references (SerializeField) ARE saved,
// so the Editor tool only needs to assign the button references; this
// component does the actual event subscription itself, every time it wakes up.

using UnityEngine;

namespace RacingPrototype
{
    public class TouchInput : MonoBehaviour, IDriveInput
    {
        [Header("On-Screen Buttons (assigned by TouchControlsFactory)")]
        [SerializeField] private HoldButton steerLeftButton;
        [SerializeField] private HoldButton steerRightButton;
        [SerializeField] private HoldButton throttleButton;
        [SerializeField] private HoldButton brakeButton;
        [SerializeField] private HoldButton reverseButton;
        [SerializeField] private HoldButton handbrakeButton;

        // Each flag is set true while its corresponding button is held down,
        // false on release. Simple digital (on/off) input - matches the
        // arcade feel and is far more reliable on a touchscreen than trying
        // to read partial press strength.
        private bool steerLeftHeld;
        private bool steerRightHeld;
        private bool throttleHeld;
        private bool brakeHeld;
        private bool reverseHeld;
        private bool handbrakeHeld;

        private void Awake()
        {
            Subscribe(steerLeftButton, () => steerLeftHeld = true, () => steerLeftHeld = false);
            Subscribe(steerRightButton, () => steerRightHeld = true, () => steerRightHeld = false);
            Subscribe(throttleButton, () => throttleHeld = true, () => throttleHeld = false);
            Subscribe(brakeButton, () => brakeHeld = true, () => brakeHeld = false);
            Subscribe(reverseButton, () => reverseHeld = true, () => reverseHeld = false);
            Subscribe(handbrakeButton, () => handbrakeHeld = true, () => handbrakeHeld = false);
        }

        private static void Subscribe(HoldButton button, System.Action onPressed, System.Action onReleased)
        {
            if (button == null)
            {
                Debug.LogWarning("TouchInput: a button reference is not assigned. " +
                                  "That control will not respond. Re-run Racing Prototype > Build Race Scene, " +
                                  "or assign it manually in the Inspector.");
                return;
            }
            button.Pressed += onPressed;
            button.Released += onReleased;
        }

        public DriveInputState GetInput()
        {
            var state = new DriveInputState();

            float steer = 0f;
            if (steerLeftHeld) steer -= 1f;
            if (steerRightHeld) steer += 1f;
            state.Steer = steer;

            state.Handbrake = handbrakeHeld;

            if (reverseHeld)
            {
                // Reverse button doubles as a brake-then-reverse input, matching
                // CarController's existing handling of DriveInputState.Reverse:
                // it brakes first if still rolling forward, then reverses once slow.
                state.Reverse = true;
                state.Brake = 1f;
                state.Throttle = 0f;
            }
            else
            {
                state.Reverse = false;
                state.Throttle = throttleHeld ? 1f : 0f;
                state.Brake = brakeHeld ? 1f : 0f;
            }

            return state;
        }

        // Safety net: if the app loses focus or a touch is dragged off a button
        // without a proper "up" event, don't leave a control stuck on.
        private void OnApplicationPause(bool paused)
        {
            if (paused) ReleaseAll();
        }

        private void OnDisable()
        {
            ReleaseAll();
        }

        private void ReleaseAll()
        {
            steerLeftHeld = false;
            steerRightHeld = false;
            throttleHeld = false;
            brakeHeld = false;
            reverseHeld = false;
            handbrakeHeld = false;
        }
    }
}
