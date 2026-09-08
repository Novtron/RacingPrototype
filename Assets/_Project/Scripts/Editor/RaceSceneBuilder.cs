// RaceSceneBuilder.cs
// EDITOR-ONLY TOOL. Menu: "Racing Prototype > Build Race Scene"
//
// Generates a complete, playable race scene from primitives: an oval track
// with checkpoints, a player car, one AI car, a chase camera, HUD, and the
// RaceManager wiring - all as real, editable GameObjects in the currently
// open scene. Run it once on an empty scene, then press Play.
//
// This is a build-time tool, not gameplay code: none of it runs in a build.
// Everything it creates is plain GameObjects + the runtime scripts in
// Scripts/Car, Scripts/AI, Scripts/Track, Scripts/Race, Scripts/UI, Scripts/Camera.

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RacingPrototype.EditorTools
{
    public static class RaceSceneBuilder
    {
        // ---- Track layout: a simple rounded-rectangle oval, hand-tuned waypoints. ----
        // Index 0 is the start/finish line. Order defines driving direction.
        private static readonly Vector3[] TrackWaypoints =
        {
            new Vector3(0f, 0f, 0f),       // 0: start/finish
            new Vector3(25f, 0f, 0f),
            new Vector3(45f, 0f, 15f),
            new Vector3(45f, 0f, 40f),
            new Vector3(30f, 0f, 55f),
            new Vector3(0f, 0f, 55f),
            new Vector3(-30f, 0f, 55f),
            new Vector3(-45f, 0f, 40f),
            new Vector3(-45f, 0f, 15f),
            new Vector3(-25f, 0f, 0f),
        };

        private const float RoadWidth = 10f;
        private const float WallHeight = 1.2f;
        private const float WallThickness = 0.4f;

        [MenuItem("Racing Prototype/Build Race Scene")]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Race Scene",
                    "This will add a full race scene (track, cars, UI, camera) into the currently open scene. " +
                    "Run this on an empty/new scene for best results. Continue?",
                    "Build", "Cancel"))
            {
                return;
            }

            BuildSceneInternal();
        }

        // Non-interactive entry point for automated/CI scene generation (e.g. GitHub Actions
        // running Unity in -batchmode). EditorUtility.DisplayDialog blocks waiting for a user
        // click and never returns in batch mode, so CI cannot call BuildScene() above directly.
        // This does exactly what BuildScene() does after its dialog is confirmed - same method,
        // same GameObjects, same wiring - just without the confirmation prompt. See
        // Assets/Editor/UnityBuilderAction/CiSceneSetup.cs for the caller.
        public static void BuildSceneNonInteractive()
        {
            BuildSceneInternal();
        }

        // Extracted verbatim from the original BuildScene() body below the dialog check.
        // No logic changed here - this refactor only splits "ask for confirmation" from
        // "do the build" so both an interactive and a non-interactive caller can share it.
        private static void BuildSceneInternal()
        {
            var root = new GameObject("RaceScene_Generated");
            Undo.RegisterCreatedObjectUndo(root, "Build Race Scene");

            EnsureEventSystem();
            BuildGround(root.transform);

            List<Checkpoint> checkpoints = BuildTrackAndCheckpoints(root.transform);
            BuildRaceTrack(root.transform, checkpoints, totalLaps: 3);

            GameObject playerCar = CarFactory.BuildCar(
                root.transform, "PlayerCar", CarFactory.BodyColorPlayer,
                spawnPos: TrackWaypoints[0] + new Vector3(-1.5f, 0.5f, -2f),
                spawnForward: (TrackWaypoints[1] - TrackWaypoints[0]).normalized,
                isPlayer: true);

            GameObject aiCar = CarFactory.BuildCar(
                root.transform, "AICar_1", CarFactory.BodyColorAI,
                spawnPos: TrackWaypoints[0] + new Vector3(1.5f, 0.5f, -4f),
                spawnForward: (TrackWaypoints[1] - TrackWaypoints[0]).normalized,
                isPlayer: false);

            BuildCamera(root.transform, playerCar.transform);
            RaceManager raceManager = BuildRaceManager(root.transform, playerCar, new[] { playerCar, aiCar });
            Transform canvasTransform = UIFactory.BuildHUD(root.transform, raceManager, playerCar);
            TouchControlsFactory.BuildTouchControls(canvasTransform, playerCar);
            BuildLight(root.transform);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("Race scene built. Press Play to test. " +
                      "Track waypoints, car tuning, and colors can all be tweaked in the Inspector.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        private static void BuildGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(20f, 1f, 20f); // 200x200 units
            ApplyColor(ground, new Color(0.25f, 0.45f, 0.2f)); // grass green
        }

        private static List<Checkpoint> BuildTrackAndCheckpoints(Transform parent)
        {
            var trackParent = new GameObject("Track").transform;
            trackParent.SetParent(parent);

            var roadParent = new GameObject("RoadSegments").transform;
            roadParent.SetParent(trackParent);

            var wallParent = new GameObject("Walls").transform;
            wallParent.SetParent(trackParent);

            var checkpointParent = new GameObject("Checkpoints").transform;
            checkpointParent.SetParent(trackParent);

            Material roadMat = CreateSimpleMaterial("Mat_Road", new Color(0.15f, 0.15f, 0.16f));
            Material wallMat = CreateSimpleMaterial("Mat_Wall", new Color(0.8f, 0.1f, 0.1f));

            int count = TrackWaypoints.Length;
            var checkpoints = new List<Checkpoint>(count);

            for (int i = 0; i < count; i++)
            {
                Vector3 a = TrackWaypoints[i];
                Vector3 b = TrackWaypoints[(i + 1) % count];
                BuildRoadSegment(roadParent, a, b, roadMat);
                BuildWallPair(wallParent, a, b, wallMat);
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = TrackWaypoints[i];
                Vector3 dirToNext = (TrackWaypoints[(i + 1) % count] - pos).normalized;
                Checkpoint cp = BuildCheckpoint(checkpointParent, pos, dirToNext, i == 0, i);
                checkpoints.Add(cp);
            }

            return checkpoints;
        }

        private static void BuildRoadSegment(Transform parent, Vector3 a, Vector3 b, Material mat)
        {
            Vector3 mid = (a + b) * 0.5f;
            float length = Vector3.Distance(a, b);

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "RoadSegment";
            seg.transform.SetParent(parent);
            seg.transform.position = mid + Vector3.up * 0.01f; // avoid z-fighting with ground
            seg.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
            seg.transform.localScale = new Vector3(RoadWidth, 0.05f, length + RoadWidth * 0.5f);
            ApplyColor(seg, mat);
        }

        private static void BuildWallPair(Transform parent, Vector3 a, Vector3 b, Material mat)
        {
            Vector3 dir = (b - a).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            float length = Vector3.Distance(a, b) + RoadWidth * 0.5f;
            Vector3 mid = (a + b) * 0.5f;

            BuildWallSegment(parent, mid + side * (RoadWidth * 0.5f), dir, length, mat, "Wall_Outer");
            BuildWallSegment(parent, mid - side * (RoadWidth * 0.5f), dir, length, mat, "Wall_Inner");
        }

        private static void BuildWallSegment(Transform parent, Vector3 center, Vector3 forward, float length, Material mat, string name)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = center + Vector3.up * (WallHeight * 0.5f);
            wall.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            wall.transform.localScale = new Vector3(WallThickness, WallHeight, length);
            ApplyColor(wall, mat);
        }

        private static Checkpoint BuildCheckpoint(Transform parent, Vector3 pos, Vector3 forward, bool isStartFinish, int index)
        {
            GameObject go = new GameObject(isStartFinish ? "Checkpoint_00_StartFinish" : $"Checkpoint_{index:00}");
            go.transform.SetParent(parent);
            go.transform.position = pos + Vector3.up * 1f;
            go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(RoadWidth, 2f, 1f);

            Checkpoint cp = go.AddComponent<Checkpoint>();

            if (isStartFinish)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "StartFinishLineVisual";
                marker.transform.SetParent(go.transform);
                marker.transform.localPosition = new Vector3(0f, -1f, 0f);
                marker.transform.localRotation = Quaternion.identity;
                marker.transform.localScale = new Vector3(RoadWidth, 0.05f, 0.5f);
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                ApplyColor(marker, CreateSimpleMaterial("Mat_StartFinish", Color.white));
            }

            return cp;
        }

        private static RaceTrack BuildRaceTrack(Transform parent, List<Checkpoint> checkpoints, int totalLaps)
        {
            GameObject go = new GameObject("RaceTrack");
            go.transform.SetParent(parent);
            RaceTrack track = go.AddComponent<RaceTrack>();
            track.Editor_SetCheckpoints(checkpoints, totalLaps);
            return track;
        }

        private static Camera BuildCamera(Transform parent, Transform target)
        {
            var existing = Object.FindFirstObjectByType<Camera>();
            if (existing != null && existing.CompareTag("MainCamera"))
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject camGO = new GameObject("RaceCamera", typeof(Camera), typeof(AudioListener), typeof(ChaseCamera));
            camGO.transform.SetParent(parent);
            camGO.tag = "MainCamera";
            camGO.transform.position = target.position + new Vector3(0f, 3.5f, -7f);

            ChaseCamera chase = camGO.GetComponent<ChaseCamera>();
            chase.SetTarget(target);

            return camGO.GetComponent<Camera>();
        }

        private static RaceManager BuildRaceManager(Transform parent, GameObject playerCar, GameObject[] allCars)
        {
            GameObject go = new GameObject("RaceManager");
            go.transform.SetParent(parent);
            RaceManager rm = go.AddComponent<RaceManager>();

            var so = new SerializedObject(rm);
            SerializedProperty racersProp = so.FindProperty("racers");
            racersProp.arraySize = allCars.Length;
            for (int i = 0; i < allCars.Length; i++)
            {
                racersProp.GetArrayElementAtIndex(i).objectReferenceValue = allCars[i].GetComponent<LapTracker>();
            }
            so.FindProperty("playerLapTracker").objectReferenceValue = playerCar.GetComponent<LapTracker>();
            so.ApplyModifiedPropertiesWithoutUndo();

            return rm;
        }

        private static void BuildLight(Transform parent)
        {
            if (Object.FindFirstObjectByType<Light>() != null) return;

            GameObject lightGO = new GameObject("Directional Light", typeof(Light));
            lightGO.transform.SetParent(parent);
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }

        // ---- Shared small helpers ----

        internal static Material CreateSimpleMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
        }

        internal static void ApplyColor(GameObject go, Color color)
        {
            ApplyColor(go, CreateSimpleMaterial("Mat_" + go.name, color));
        }

        internal static void ApplyColor(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
        }
    }
}
#endif
