// CarFactory.cs
// EDITOR-ONLY. Builds a complete car from primitives: chassis collider,
// 4 WheelColliders (physics), 4 wheel visual meshes, a body mesh, and
// wires up CarController + LapTracker + (TouchInput for the player, or
// AIDriver for opponents).
//
// All cosmetic meshes are parented under a CarVisualRoot child so they can
// be deleted and replaced with a Tripo AI model later without touching
// CarController, LapTracker, or input scripts.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RacingPrototype.EditorTools
{
    internal static class CarFactory
    {
        public static readonly Color BodyColorPlayer = new Color(0.15f, 0.5f, 0.9f);
        public static readonly Color BodyColorAI = new Color(0.9f, 0.3f, 0.1f);

        private const float BodyLength = 3.6f;
        private const float BodyWidth = 1.8f;
        private const float BodyHeight = 0.9f;
        private const float WheelRadius = 0.4f;
        private const float WheelWidth = 0.3f;
        private const float WheelbaseHalf = 1.2f;   // distance from center to front/rear axle
        private const float TrackWidthHalf = 0.85f; // distance from center to left/right wheel

        public static GameObject BuildCar(Transform parent, string name, Color bodyColor,
            Vector3 spawnPos, Vector3 spawnForward, bool isPlayer)
        {
            GameObject car = new GameObject(name);
            car.transform.SetParent(parent);
            car.transform.position = spawnPos;
            car.transform.rotation = spawnForward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(spawnForward, Vector3.up)
                : Quaternion.identity;

            Rigidbody rb = car.AddComponent<Rigidbody>();
            rb.mass = 1200f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;

            BoxCollider chassisCollider = car.AddComponent<BoxCollider>();
            chassisCollider.center = new Vector3(0f, BodyHeight * 0.5f + 0.15f, 0f);
            chassisCollider.size = new Vector3(BodyWidth, BodyHeight, BodyLength);

            // ---- Visuals (swappable later) ----
            GameObject visualRoot = new GameObject("Visuals");
            visualRoot.transform.SetParent(car.transform);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.AddComponent<CarVisualRoot>();

            Material bodyMat = RaceSceneBuilder.CreateSimpleMaterial($"Mat_{name}_Body", bodyColor);
            BuildBodyVisual(visualRoot.transform, bodyMat);

            // ---- Wheel colliders (physics) - separate GameObjects per Unity's WheelCollider requirements ----
            WheelCollider wcFL = BuildWheelCollider(car.transform, "WheelCollider_FL", new Vector3(-TrackWidthHalf, WheelRadius, WheelbaseHalf));
            WheelCollider wcFR = BuildWheelCollider(car.transform, "WheelCollider_FR", new Vector3(TrackWidthHalf, WheelRadius, WheelbaseHalf));
            WheelCollider wcRL = BuildWheelCollider(car.transform, "WheelCollider_RL", new Vector3(-TrackWidthHalf, WheelRadius, -WheelbaseHalf));
            WheelCollider wcRR = BuildWheelCollider(car.transform, "WheelCollider_RR", new Vector3(TrackWidthHalf, WheelRadius, -WheelbaseHalf));

            // ---- Wheel visuals (cosmetic cylinders, driven every frame from wheel collider pose) ----
            Material wheelMat = RaceSceneBuilder.CreateSimpleMaterial("Mat_WheelDark", new Color(0.08f, 0.08f, 0.08f));
            Transform wvFL = BuildWheelVisual(visualRoot.transform, "WheelVisual_FL", wheelMat);
            Transform wvFR = BuildWheelVisual(visualRoot.transform, "WheelVisual_FR", wheelMat);
            Transform wvRL = BuildWheelVisual(visualRoot.transform, "WheelVisual_RL", wheelMat);
            Transform wvRR = BuildWheelVisual(visualRoot.transform, "WheelVisual_RR", wheelMat);

            // ---- Gameplay components ----
            // Player uses on-screen touch controls (this project targets Android
            // as the primary platform). TouchInput is wired to the on-screen
            // buttons by TouchControlsFactory after BuildHUD runs.
            if (isPlayer)
            {
                car.AddComponent<TouchInput>();
            }
            else
            {
                car.AddComponent<AIDriver>();
            }

            CarController controller = car.AddComponent<CarController>();
            WireCarController(controller, wcFL, wcFR, wcRL, wcRR, wvFL, wvFR, wvRL, wvRR);

            LapTracker lapTracker = car.AddComponent<LapTracker>();
            var so = new SerializedObject(lapTracker);
            so.FindProperty("driverName").stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();

            return car;
        }

        private static void BuildBodyVisual(Transform parent, Material bodyMat)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, BodyHeight * 0.5f + 0.15f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(BodyWidth, BodyHeight, BodyLength);
            Object.DestroyImmediate(body.GetComponent<Collider>()); // chassis collider on root handles physics
            RaceSceneBuilder.ApplyColor(body, bodyMat);

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(parent);
            cabin.transform.localPosition = new Vector3(0f, BodyHeight + 0.45f, -0.2f);
            cabin.transform.localRotation = Quaternion.identity;
            cabin.transform.localScale = new Vector3(BodyWidth * 0.75f, 0.5f, BodyLength * 0.45f);
            Object.DestroyImmediate(cabin.GetComponent<Collider>());
            RaceSceneBuilder.ApplyColor(cabin, bodyMat);
        }

        private static WheelCollider BuildWheelCollider(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            WheelCollider wc = go.AddComponent<WheelCollider>();
            wc.radius = WheelRadius;
            wc.mass = 20f;
            wc.suspensionDistance = 0.25f;
            wc.forceAppPointDistance = 0.1f;

            JointSpring spring = wc.suspensionSpring;
            spring.spring = 35000f;
            spring.damper = 4500f;
            spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;

            WheelFrictionCurve fwdFriction = wc.forwardFriction;
            fwdFriction.extremumSlip = 0.4f;
            fwdFriction.extremumValue = 1f;
            fwdFriction.asymptoteSlip = 0.8f;
            fwdFriction.asymptoteValue = 0.5f;
            fwdFriction.stiffness = 2.2f;
            wc.forwardFriction = fwdFriction;

            WheelFrictionCurve sideFriction = wc.sidewaysFriction;
            sideFriction.extremumSlip = 0.3f;
            sideFriction.extremumValue = 1f;
            sideFriction.asymptoteSlip = 0.5f;
            sideFriction.asymptoteValue = 0.75f;
            sideFriction.stiffness = 2.0f;
            wc.sidewaysFriction = sideFriction;

            return wc;
        }

        private static Transform BuildWheelVisual(Transform parent, string name, Material wheelMat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            // Cylinder's default long axis is local Y; rotate so it becomes the car's X (axle) axis.
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            go.transform.localScale = new Vector3(WheelRadius * 2f, WheelWidth * 0.5f, WheelRadius * 2f);
            Object.DestroyImmediate(go.GetComponent<Collider>()); // WheelCollider handles physics, not this mesh
            RaceSceneBuilder.ApplyColor(go, wheelMat);
            return go.transform;
        }

        private static void WireCarController(CarController controller,
            WheelCollider fl, WheelCollider fr, WheelCollider rl, WheelCollider rr,
            Transform wvFL, Transform wvFR, Transform wvRL, Transform wvRR)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("wheelColliderFL").objectReferenceValue = fl;
            so.FindProperty("wheelColliderFR").objectReferenceValue = fr;
            so.FindProperty("wheelColliderRL").objectReferenceValue = rl;
            so.FindProperty("wheelColliderRR").objectReferenceValue = rr;
            so.FindProperty("wheelVisualFL").objectReferenceValue = wvFL;
            so.FindProperty("wheelVisualFR").objectReferenceValue = wvFR;
            so.FindProperty("wheelVisualRL").objectReferenceValue = wvRL;
            so.FindProperty("wheelVisualRR").objectReferenceValue = wvRR;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
