// CarVisualRoot.cs
// Marker component with no logic. Its GameObject is the single parent for
// every cosmetic mesh on a car (body, wheel visuals). To swap in a Tripo AI
// generated car model later:
//   1. Delete all children of the GameObject holding this component.
//   2. Parent the new model's meshes underneath it instead.
//   3. Re-point CarController's wheelVisualFL/FR/RL/RR to the new wheel meshes.
// No gameplay script needs to change.

using UnityEngine;

namespace RacingPrototype
{
    public class CarVisualRoot : MonoBehaviour
    {
    }
}
