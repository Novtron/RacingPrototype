// HoldButton.cs
// A UI button that reports "pressed" / "released" instead of "clicked".
// Standard uGUI Button only fires onClick on a full press-then-release inside
// the same button, which is wrong for driving controls (you want throttle to
// stay on for as long as the finger is down, and to turn off if the finger
// slides off the button rather than lifting cleanly).
//
// Implements the pointer interfaces directly (no EventTrigger component
// needed). TouchInput subscribes to Pressed/Released itself at runtime -
// see the comment at the top of TouchInput.cs for why that subscription
// can't happen from editor tooling.

using UnityEngine;
using UnityEngine.EventSystems;

namespace RacingPrototype
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event System.Action Pressed;
        public event System.Action Released;

        private bool isPressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isPressed) return;
            isPressed = true;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            RaiseReleaseIfNeeded();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Finger dragged off the button without lifting: treat as released
            // so the control doesn't get stuck "on".
            RaiseReleaseIfNeeded();
        }

        private void OnDisable()
        {
            RaiseReleaseIfNeeded();
        }

        private void RaiseReleaseIfNeeded()
        {
            if (!isPressed) return;
            isPressed = false;
            Released?.Invoke();
        }
    }
}
