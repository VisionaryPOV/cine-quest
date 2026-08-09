#if UNITY_EDITOR
// Cine Quest — One-click project setup reminders and validation.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CineQuest.EditorTools
{
    public sealed class CineQuestSetupWizard : EditorWindow
    {
        Vector2 _scroll;

        [MenuItem("Cine Quest/Setup Wizard…", false, 1)]
        public static void Open()
        {
            var w = GetWindow<CineQuestSetupWizard>("Cine Quest Setup");
            w.minSize = new Vector2(480, 520);
            w.Show();
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Label("Cine Quest — Setup Checklist", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawStep("1. Unity version", "Use Unity 6000.0.60f1 (Unity 6 LTS) from Unity Hub.");
            DrawStep("2. Packages", "Wait for URP / OpenXR / Input System to resolve. Then add Meta XR All-in-One (com.meta.xr.sdk.all).");
            DrawStep("3. UVC plugin", "Import UVC4UnityAndroid. Add scripting define CINE_QUEST_UVC4UNITY for Android.");
            DrawStep("4. Main scene", "Open Assets/CineQuest/Scenes/Main_CineQuest.unity or use Cine Quest → Create Main Scene.");
            DrawStep("5. XR", "XR Plug-in Management → Android → OpenXR + Meta features. Enable Passthrough & Hand Tracking.");
            DrawStep("6. Android", "IL2CPP, ARM64, GLES3, Min API 32, custom main manifest enabled.");
            DrawStep("7. Fidelity", "Disable URP post-processing on XR cameras. Use Bypass mode for reference.");
            DrawStep("8. Build", "Build APK → adb install / MQDH. See Docs/BUILD_AND_DEPLOY.md.");

            EditorGUILayout.Space(12);
            GUILayout.Label("Project health", EditorStyles.boldLabel);

            bool hasManifest = File.Exists("Assets/Plugins/Android/AndroidManifest.xml");
            bool hasLockedShader = File.Exists("Assets/CineQuest/Shaders/LockedVideo.shader");
            bool hasCompute = File.Exists("Assets/CineQuest/Resources/Compute/ScopeWaveform.compute");
            bool hasScene = File.Exists("Assets/CineQuest/Scenes/Main_CineQuest.unity");
            bool hasReadme = File.Exists("README.md");

            Status("AndroidManifest", hasManifest);
            Status("LockedVideo shader", hasLockedShader);
            Status("Scope compute (Resources)", hasCompute);
            Status("Main scene", hasScene);
            Status("README", hasReadme);

            var defines = PlayerSettings.GetScriptingDefineSymbols(
                UnityEditor.Build.NamedBuildTarget.Android);
            bool hasUvcDefine = defines.Split(';').Any(d => d.Trim() == "CINE_QUEST_UVC4UNITY");
            Status("CINE_QUEST_UVC4UNITY define (Android)", hasUvcDefine);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Create / Refresh Main Scene"))
                CineQuestEditorMenu.CreateMainScene();
            if (GUILayout.Button("Validate Fidelity Settings"))
                CineQuestEditorMenu.ValidateFidelity();
            if (GUILayout.Button("Open Docs Folder"))
                CineQuestEditorMenu.OpenDocs();
            if (GUILayout.Button("Add CINE_QUEST_UVC4UNITY Define (Android)"))
            {
                if (!hasUvcDefine)
                {
                    var next = string.IsNullOrEmpty(defines) ? "CINE_QUEST_UVC4UNITY" : defines + ";CINE_QUEST_UVC4UNITY";
                    PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Android, next);
                    Debug.Log("[CineQuest] Added CINE_QUEST_UVC4UNITY to Android scripting defines.");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        static void DrawStep(string title, string body)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(body, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(4);
        }

        static void Status(string label, bool ok)
        {
            var c = GUI.color;
            GUI.color = ok ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.55f, 0.4f);
            EditorGUILayout.LabelField(ok ? "✓" : "✗", label);
            GUI.color = c;
        }
    }
}
#endif
