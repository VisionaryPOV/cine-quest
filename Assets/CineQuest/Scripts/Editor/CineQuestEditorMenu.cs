#if UNITY_EDITOR
// Cine Quest — Editor utilities for project setup.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CineQuest.App;

namespace CineQuest.EditorTools
{
    public static class CineQuestEditorMenu
    {
        const string ScenePath = "Assets/CineQuest/Scenes/Main_CineQuest.unity";

        [MenuItem("Cine Quest/Create Main Scene With Bootstrap", false, 0)]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var bootstrap = new GameObject("CineQuest_Bootstrap");
            bootstrap.AddComponent<RuntimeSceneBuilder>();
            bootstrap.AddComponent<CineQuestApp>();

            // Remove default directional light influence on monitoring (keep optional)
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                    light.intensity = 0.15f;
            }

            System.IO.Directory.CreateDirectory("Assets/CineQuest/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Debug.Log($"[CineQuest] Scene saved: {ScenePath}. Press Play — RuntimeSceneBuilder builds the full hierarchy.");
        }

        [MenuItem("Cine Quest/Open Documentation Folder", false, 20)]
        public static void OpenDocs()
        {
            var path = System.IO.Path.GetFullPath("Docs");
            if (System.IO.Directory.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                Debug.LogWarning("Docs folder not found.");
        }

        [MenuItem("Cine Quest/Validate Fidelity Settings", false, 10)]
        public static void ValidateFidelity()
        {
            int issues = 0;
            foreach (var cam in Camera.allCameras)
            {
                var urp = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (urp != null && urp.renderPostProcessing)
                {
                    Debug.LogWarning($"Camera {cam.name}: post-processing ON", cam);
                    issues++;
                }
            }
            if (issues == 0)
                EditorUtility.DisplayDialog("Cine Quest", "No URP post-processing issues found on cameras in open scenes.", "OK");
            else
                EditorUtility.DisplayDialog("Cine Quest", $"{issues} camera(s) have post-processing enabled. Disable for signal fidelity.", "OK");
        }
    }
}
#endif
