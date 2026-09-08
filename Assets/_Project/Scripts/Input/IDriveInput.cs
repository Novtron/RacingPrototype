// IDriveInput.cs
// Any control scheme (keyboard, AI, future touch) implements this and feeds CarController.
// This is the seam that lets touch controls be added later without touching car physics.

namespace RacingPrototype
{
    public struct DriveInputState
    {
        public float Throttle;   // 0..1
        public float Brake;      // 0..1
        public float Steer;      // -1..1 (left..right)
        public bool Handbrake;
        public bool Reverse;     // true = requesting reverse gear
    }

    public interface IDriveInput
    {
        DriveInputState GetInput();
    }
}
