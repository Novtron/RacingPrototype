// This file is CI-only infrastructure. It is not part of the game's gameplay code and
// does not change any gameplay logic. It exists solely so that GitHub Actions (running
// Unity in -batchmode, with no human available to click anything) can generate the race
// scene the same way the "Racing Prototype > Build Race Scene" editor menu does, save it
// to disk, and register it in the project's build settings - all before the actual
// Android player build runs as a separate workflow step.
//
// See: Assets/_Project/Scripts/Editor/RaceSceneBuilder.cs (BuildSceneNonInteractive)
// See: .github/workflows/build-apk.yml (the "Generate Race Scene" step calls
//      CiSceneSetup.GenerateAndSaveScene as a custom -executeMethod / buildMethod)
using System;
using RacingPrototype.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CI
{
    public static class CiSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/RaceScene.unity";

        // Invoked by game-ci/unity-builder as a custom buildMethod (see workflow file).
        // This method explicitly calls EditorApplication.Exit() itself on both the success
        // and failure paths below, rather than relying on Unity's -quit command-line flag
        // (which unity-builder may or may not still append when a custom buildMethod is set).
        public static void GenerateAndSaveScene()
        {
            try
            {
                Debug.Log("[CI] Creating a new empty scene for the race scene builder to populate...");
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                Debug.Log("[CI] Invoking RaceSceneBuilder.BuildSceneNonInteractive() " +
                          "(same generation logic as the editor menu, without the confirmation dialog)...");
                RaceSceneBuilder.BuildSceneNonInteractive();

                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(ScenePath) ?? "Assets/_Project/Scenes");

                Debug.Log($"[CI] Saving generated scene to {ScenePath}...");
                bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
                if (!saved)
                {
                    throw new Exception($"EditorSceneManager.SaveScene returned false for path: {ScenePath}");
                }

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
                bool alreadyRegistered = Array.Exists(existingScenes, s => s.path == ScenePath);
                if (!alreadyRegistered)
                {
                    Debug.Log("[CI] Registering scene in Build Settings...");
                    var newScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
                    Array.Copy(existingScenes, newScenes, existingScenes.Length);
                    newScenes[existingScenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
                    EditorBuildSettings.scenes = newScenes;
                }

                Debug.Log("[CI] Race scene generated and saved successfully.");

                // Defensive: this workflow step's real purpose is generating the scene, not
                // producing a player build, so there is nothing for unity-builder to put in
                // buildsPath/Android/. Some versions of the action check that this folder is
                // non-empty after a build step runs, so a harmless marker file is written here
                // to satisfy that check without affecting anything else.
                string ciMarkerDir = "build-scene-step/Android";
                System.IO.Directory.CreateDirectory(ciMarkerDir);
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(ciMarkerDir, "scene-generation-step.txt"),
                    "Scene generation step completed successfully. " +
                    "The actual Android APK is produced by the next workflow step.");

                // Explicitly exit with success. unity-builder normally appends -quit to the
                // Unity command line automatically, but relying on that implicitly for a custom
                // buildMethod is a known source of CI jobs hanging until they time out - calling
                // Exit ourselves guarantees Unity actually terminates here.
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                // A non-zero exit code here fails the GitHub Actions step clearly and loudly,
                // instead of silently producing a build with no scene in it.
                Debug.LogError($"[CI] Scene generation failed: {ex}");
                EditorApplication.Exit(1);
            }
        }
    }
}
