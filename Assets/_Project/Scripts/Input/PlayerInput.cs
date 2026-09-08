// PlayerInput.cs
// Keyboard control scheme. Uses the legacy Input Manager (Input.GetAxis) so the
// project works with zero extra package setup. Implements IDriveInput so
// CarController never needs to know where input came from.
//
// Controls:
//   W / Up Arrow    = throttle
//   S / Down Arrow  = brake, or reverse if car is stopped/slow
//   A/D, Left/Right = steer
//   Space           = handbrake

using UnityEngine;

namespace RacingPrototype
{
    public class PlayerInput : MonoBehaviour, IDriveInput
    {
        [SerializeField] private string throttleBrakeAxis = "Vertical";
        [SerializeField] private string steerAxis = "Horizontal";
        [SerializeField] private KeyCode handbrakeKey = KeyCode.Space;

        public DriveInputState GetInput()
        {
            float vertical = Input.GetAxis(throttleBrakeAxis);
            float horizontal = Input.GetAxis(steerAxis);
            bool handbrake = Input.GetKey(handbrakeKey);

            var state = new DriveInputState
            {
                Steer = horizontal,
                Handbrake = handbrake
            };

            if (vertical >= 0f)
            {
                state.Throttle = vertical;
                state.Brake = 0f;
                state.Reverse = false;
            }
            else
            {
                // Negative vertical input is interpreted by CarController as
                // brake-while-moving-forward, or reverse-throttle once slow enough.
                state.Throttle = 0f;
                state.Brake = -vertical;
                state.Reverse = true;
            }

            return state;
        }
    }
}
