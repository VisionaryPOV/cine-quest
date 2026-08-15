// Cine Quest — Builds a usable main scene hierarchy at runtime if prefabs are not wired.
// Open Main_CineQuest scene (or empty scene + this component), press Play.

using CineQuest.Audio;
using CineQuest.Capture;
using CineQuest.Diagnostics;
using CineQuest.Persistence;
using CineQuest.Scopes;
using CineQuest.UI;
using CineQuest.Video;
using CineQuest.XR;
using UnityEngine;
using UnityEngine.UI;

namespace CineQuest.App
{
    [DefaultExecutionOrder(-300)]
    public sealed class RuntimeSceneBuilder : MonoBehaviour
    {
        [SerializeField] bool buildOnAwake = true;
        [SerializeField] bool createEventSystem = true;

        static bool _built;

        void Awake()
        {
            if (buildOnAwake)
                Build();
        }

        [ContextMenu("Build Cine Quest Hierarchy")]
        public void Build()
        {
            if (_built && Application.isPlaying)
            {
                // Avoid duplicate hierarchy if domain reload is off
                if (GameObject.Find("CineQuest_Root") != null) return;
            }
            _built = true;

            var root = GameObject.Find("CineQuest_Root") ?? new GameObject("CineQuest_Root");

            // Camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
                cam.transform.position = new Vector3(0, 1.4f, 0);
            }
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 0f);

            // Capture
            var capGo = EnsureChild(root, "CaptureService");
            var capture = capGo.GetComponent<CaptureService>() ?? capGo.AddComponent<CaptureService>();

            // Video panel
            var panelGo = EnsureChild(root, "VideoMonitorPanel");
            panelGo.transform.position = cam.transform.position + cam.transform.forward * 1.6f + Vector3.up * 0.05f;
            panelGo.transform.rotation = Quaternion.LookRotation(panelGo.transform.position - cam.transform.position);
            panelGo.transform.Rotate(0, 180, 0);
            panelGo.transform.localScale = Vector3.one * 1.2f;

            var panel = panelGo.GetComponent<VideoPanel>() ?? panelGo.AddComponent<VideoPanel>();
            var locked = panelGo.GetComponent<LockedVideoRenderer>() ?? panelGo.AddComponent<LockedVideoRenderer>();
            var imgCtrl = panelGo.GetComponent<ImageParameterController>() ?? panelGo.AddComponent<ImageParameterController>();
            panelGo.GetComponent<SimpleGrabTransform>() ?? panelGo.AddComponent<SimpleGrabTransform>();

            var shader = Shader.Find("CineQuest/LockedVideo");
            Material mat = null;
            if (shader != null)
            {
                mat = new Material(shader) { name = "LockedVideo_Runtime" };
                var mr = panelGo.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = mat;
            }
            imgCtrl.SetMaterial(locked.Material ?? mat);

            var freeze = panelGo.GetComponent<FreezeFrameController>() ?? panelGo.AddComponent<FreezeFrameController>();
            freeze.Bind(capture, locked);
            locked.Bind(capture, imgCtrl, mat, freeze);

            // False color overlay
            var fcGo = EnsureChild(panelGo, "FalseColorOverlay");
            fcGo.transform.localPosition = new Vector3(0, 0, -0.001f);
            fcGo.transform.localRotation = Quaternion.identity;
            fcGo.transform.localScale = Vector3.one;
            var fcMf = fcGo.GetComponent<MeshFilter>() ?? fcGo.AddComponent<MeshFilter>();
            var panelMf = panelGo.GetComponent<MeshFilter>();
            if (panelMf != null) fcMf.sharedMesh = panelMf.sharedMesh;
            var fcMr = fcGo.GetComponent<MeshRenderer>() ?? fcGo.AddComponent<MeshRenderer>();
            fcMr.enabled = false;
            var fc = fcGo.GetComponent<FalseColorController>() ?? fcGo.AddComponent<FalseColorController>();

            // Video status overlay (world canvas on panel)
            var statusOverlay = BuildVideoStatusOverlay(panelGo, capture);

            // Theater
            var theaterGo = EnsureChild(root, "TheaterMode");
            var theater = theaterGo.GetComponent<TheaterModeController>() ?? theaterGo.AddComponent<TheaterModeController>();
            var theaterEnv = EnsureChild(theaterGo, "TheaterEnvironment");
            var wall = EnsureChild(theaterEnv, "DarkWall");
            wall.transform.localPosition = new Vector3(0, 1.4f, 4f);
            wall.transform.localScale = new Vector3(12, 6, 0.1f);
            var wallMf = wall.GetComponent<MeshFilter>() ?? wall.AddComponent<MeshFilter>();
            wallMf.sharedMesh = CreateCubeMesh();
            var wallMr = wall.GetComponent<MeshRenderer>() ?? wall.AddComponent<MeshRenderer>();
            var unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (unlit != null)
                wallMr.sharedMaterial = new Material(unlit) { color = Color.black };
            theaterEnv.SetActive(false);
            theater.Bind(panel, theaterEnv);

            // Scopes
            var scopesRoot = EnsureChild(root, "Scopes");
            var scopeMgr = scopesRoot.GetComponent<ScopeManager>() ?? scopesRoot.AddComponent<ScopeManager>();

            var wfGo = CreateScopePanel(scopesRoot, "Waveform", new Vector3(-0.9f, 1.1f, 1.4f), ScopeType.Waveform);
            var paradeGo = CreateScopePanel(scopesRoot, "Parade", new Vector3(0.9f, 1.1f, 1.4f), ScopeType.RgbParade);
            var vecGo = CreateScopePanel(scopesRoot, "Vectorscope", new Vector3(0f, 0.55f, 1.5f), ScopeType.Vectorscope);
            var histGo = CreateScopePanel(scopesRoot, "Histogram", new Vector3(0.9f, 0.55f, 1.5f), ScopeType.Histogram);
            paradeGo.SetActive(false);
            vecGo.SetActive(false);
            histGo.SetActive(false);

            var wf = wfGo.GetComponent<WaveformScope>() ?? wfGo.AddComponent<WaveformScope>();
            var parade = paradeGo.GetComponent<ParadeScope>() ?? paradeGo.AddComponent<ParadeScope>();
            var vec = vecGo.GetComponent<VectorscopeScope>() ?? vecGo.AddComponent<VectorscopeScope>();
            var hist = histGo.GetComponent<HistogramScope>() ?? histGo.AddComponent<HistogramScope>();
            wf.SetTargetRenderer(wfGo.GetComponent<MeshRenderer>());
            parade.SetTargetRenderer(paradeGo.GetComponent<MeshRenderer>());
            vec.SetTargetRenderer(vecGo.GetComponent<MeshRenderer>());
            hist.SetTargetRenderer(histGo.GetComponent<MeshRenderer>());

            var wfCs = Resources.Load<ComputeShader>("Compute/ScopeWaveform");
            var paradeCs = Resources.Load<ComputeShader>("Compute/ScopeParade");
            var vecCs = Resources.Load<ComputeShader>("Compute/ScopeVectorscope");
            scopeMgr.Bind(capture, freeze, wf, parade, vec, hist, wfCs, paradeCs, vecCs);
            scopeMgr.AutoFindScopes();

            // UI
            var uiRoot = EnsureChild(root, "UI");
            var canvasGo = EnsureChild(uiRoot, "MonitorMenu");
            var canvas = canvasGo.GetComponent<Canvas>() ?? canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
            canvasGo.GetComponent<GraphicRaycaster>() ?? canvasGo.AddComponent<GraphicRaycaster>();
            var menuRt = canvasGo.GetComponent<RectTransform>();
            // Compact operator sheet off-axis — not a 1 m slab in the picture.
            canvasGo.transform.position = cam.transform.position + cam.transform.forward * 0.62f +
                                          cam.transform.right * -0.38f + Vector3.up * -0.12f;
            canvasGo.transform.rotation = Quaternion.LookRotation(canvasGo.transform.position - cam.transform.position);
            menuRt.sizeDelta = new Vector2(720, 900);
            menuRt.localScale = Vector3.one * 0.00048f;

            var cg = canvasGo.GetComponent<CanvasGroup>() ?? canvasGo.AddComponent<CanvasGroup>();

            var bg = EnsureChild(canvasGo, "Background");
            var bgImg = bg.GetComponent<Image>() ?? bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            RuntimeUiFactory.CreateLabel(canvasGo.transform, "Title", "CINE QUEST",
                new Vector2(0, 500), new Vector2(860, 40), 30, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.9f, 1f));
            RuntimeUiFactory.CreateLabel(canvasGo.transform, "Subtitle", "LOCKED MONITORING · NO AUTO IMAGE PROCESSING",
                new Vector2(0, 465), new Vector2(860, 28), 12, TextAnchor.MiddleCenter,
                new Color(0.55f, 0.6f, 0.65f));

            // Top action buttons
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnLock", "LOCK", new Vector2(-280, 410),
                new Vector2(160, 42), () => imgCtrl.SetLocked(!imgCtrl.IsLocked));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnBypass", "BYPASS", new Vector2(-100, 410),
                new Vector2(160, 42), () => imgCtrl.SetBypass(!imgCtrl.IsBypass));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnTheater", "THEATER", new Vector2(80, 410),
                new Vector2(160, 42), () => theater.Toggle());
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnFreeze", "FREEZE", new Vector2(260, 410),
                new Vector2(160, 42), () => freeze.Toggle());

            // Scope toggles
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnWave", "WAVEFORM", new Vector2(-280, 355),
                new Vector2(160, 38), () => wfGo.SetActive(!wfGo.activeSelf));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnParade", "PARADE", new Vector2(-100, 355),
                new Vector2(160, 38), () => paradeGo.SetActive(!paradeGo.activeSelf));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnVec", "VECTOR", new Vector2(80, 355),
                new Vector2(160, 38), () => vecGo.SetActive(!vecGo.activeSelf));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnHist", "HIST", new Vector2(260, 355),
                new Vector2(160, 38), () => histGo.SetActive(!histGo.activeSelf));

            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnFC", "FALSE COLOR", new Vector2(-280, 305),
                new Vector2(160, 38), () => fc.Toggle());
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnRange", "LIM/FULL", new Vector2(-100, 305),
                new Vector2(160, 38), () =>
                {
                    var p = imgCtrl.Parameters;
                    if (p == null) return;
                    imgCtrl.SetColorSpace(p.colorSpace == VideoColorSpace.Rec709Limited
                        ? VideoColorSpace.FullRange
                        : VideoColorSpace.Rec709Limited);
                });
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnQual", "SCOPE Q", new Vector2(80, 305),
                new Vector2(160, 38), () =>
                {
                    var q = (int)scopeMgr.QualityMode;
                    scopeMgr.QualityMode = (ScopeQualityMode)((q + 1) % 3);
                    Debug.Log($"[CineQuest] Scope quality: {scopeMgr.QualityMode}");
                });
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnPulse", "PULSE TEST", new Vector2(260, 305),
                new Vector2(160, 38), () => capture.SetSyntheticPattern(SyntheticPattern.CheckerPulse));

            // Image parameter sliders
            RuntimeUiFactory.CreateLabel(canvasGo.transform, "ParamsHeader", "IMAGE PARAMETERS (disabled when LOCKED)",
                new Vector2(0, 255), new Vector2(820, 28), 13, TextAnchor.MiddleCenter,
                new Color(0.6f, 0.65f, 0.7f));

            float sy = 210f;
            float sstep = 42f;
            // Sliders owned by MonitorMenuController (no direct TrySet) so Lock/presets stay correct.
            var (brS, brT) = RuntimeUiFactory.CreateSliderRow(canvasGo.transform, "SBright", "Brightness",
                new Vector2(0, sy), -1f, 1f, 0f, null);
            sy -= sstep;
            var (ctS, ctT) = RuntimeUiFactory.CreateSliderRow(canvasGo.transform, "SContrast", "Contrast",
                new Vector2(0, sy), 0f, 2f, 1f, null);
            sy -= sstep;
            var (gmS, gmT) = RuntimeUiFactory.CreateSliderRow(canvasGo.transform, "SGamma", "Gamma",
                new Vector2(0, sy), 0.1f, 3f, 1f, null);
            sy -= sstep;
            var (satS, satT) = RuntimeUiFactory.CreateSliderRow(canvasGo.transform, "SSat", "Saturation",
                new Vector2(0, sy), 0f, 2f, 1f, null);
            sy -= sstep;
            var (tmpS, tmpT) = RuntimeUiFactory.CreateSliderRow(canvasGo.transform, "STemp", "Temperature",
                new Vector2(0, sy), -1f, 1f, 0f, null);
            sy -= sstep;
            var (tintS, tintT) = RuntimeUiFactory.CreateSliderRow(canvasGo.transform, "STint", "Tint",
                new Vector2(0, sy), -1f, 1f, 0f, null);
            sy -= sstep;
            var (liftS, liftT) = RuntimeUiFactory.CreateSliderRow(canvasGo.transform, "SLift", "Lift / Black",
                new Vector2(0, sy), -0.5f, 0.5f, 0f, null);

            // Presets
            RuntimeUiFactory.CreateLabel(canvasGo.transform, "PresetHeader", "PRESETS",
                new Vector2(0, -100), new Vector2(820, 28), 13, TextAnchor.MiddleCenter,
                new Color(0.6f, 0.65f, 0.7f));

            var menu = canvasGo.GetComponent<MonitorMenuController>() ?? canvasGo.AddComponent<MonitorMenuController>();

            RuntimeUiFactory.CreateButton(canvasGo.transform, "PNeutral", "NEUTRAL LOCK", new Vector2(-280, -145),
                new Vector2(170, 40), () => menu.ApplyPreset(PresetLibrary.NeutralLock));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "PIris", "IRIS EVAL", new Vector2(-90, -145),
                new Vector2(170, 40), () => menu.ApplyPreset(PresetLibrary.IrisEvaluation));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "PLight", "LIGHT BAL", new Vector2(100, -145),
                new Vector2(170, 40), () => menu.ApplyPreset(PresetLibrary.LightingBalance));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "PSkin", "SKIN TONE", new Vector2(290, -145),
                new Vector2(150, 40), () => menu.ApplyPreset(PresetLibrary.SkinToneCheck));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "PBypass", "REF BYPASS", new Vector2(-90, -195),
                new Vector2(170, 40), () => menu.ApplyPreset(PresetLibrary.ReferenceBypass));
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnSave", "SAVE LAYOUT", new Vector2(100, -195),
                new Vector2(170, 40), () => menu.SaveLayout());
            RuntimeUiFactory.CreateButton(canvasGo.transform, "BtnLoad", "LOAD LAYOUT", new Vector2(290, -195),
                new Vector2(150, 40), () => menu.LoadLayout());

            RuntimeUiFactory.CreateLabel(canvasGo.transform, "Help",
                "Quest: A Bypass · B Lock · Menu toggles this sheet · stick click Theater · L-grip+trigger Freeze\n" +
                "Editor: M menu · L lock · B bypass · T theater · F freeze · 4 pulse",
                new Vector2(0, -280), new Vector2(680, 70), 16, TextAnchor.MiddleCenter,
                new Color(0.7f, 0.75f, 0.78f));

            // Status HUD
            var hudGo = EnsureChild(uiRoot, "StatusHUD");
            hudGo.transform.position = cam.transform.position + cam.transform.forward * 0.95f + Vector3.up * -0.22f;
            var hudCanvas = hudGo.GetComponent<Canvas>() ?? hudGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.WorldSpace;
            hudGo.GetComponent<CanvasScaler>() ?? hudGo.AddComponent<CanvasScaler>();
            var hudRt = hudGo.GetComponent<RectTransform>();
            hudRt.sizeDelta = new Vector2(900, 100);
            hudRt.localScale = Vector3.one * 0.001f;
            var hudBg = EnsureChild(hudGo, "Bg");
            var hudBgImg = hudBg.GetComponent<Image>() ?? hudBg.AddComponent<Image>();
            hudBgImg.color = new Color(0.02f, 0.03f, 0.04f, 0.75f);
            var hudBgRt = hudBg.GetComponent<RectTransform>();
            hudBgRt.anchorMin = Vector2.zero;
            hudBgRt.anchorMax = Vector2.one;
            hudBgRt.offsetMin = Vector2.zero;
            hudBgRt.offsetMax = Vector2.zero;

            var hud = hudGo.GetComponent<StatusHud>() ?? hudGo.AddComponent<StatusHud>();
            var res = RuntimeUiFactory.CreateLabel(hudGo.transform, "Resolution", "—", new Vector2(-300, 18),
                new Vector2(180, 28), 16, TextAnchor.MiddleLeft);
            var fps = RuntimeUiFactory.CreateLabel(hudGo.transform, "FPS", "— fps", new Vector2(-120, 18),
                new Vector2(120, 28), 16, TextAnchor.MiddleLeft);
            var usb = RuntimeUiFactory.CreateLabel(hudGo.transform, "USB", "USB ?", new Vector2(40, 18),
                new Vector2(200, 28), 16, TextAnchor.MiddleLeft);
            var lat = RuntimeUiFactory.CreateLabel(hudGo.transform, "Latency", "~0 ms", new Vector2(260, 18),
                new Vector2(120, 28), 16, TextAnchor.MiddleLeft);
            var fmt = RuntimeUiFactory.CreateLabel(hudGo.transform, "Format", "—", new Vector2(-300, -18),
                new Vector2(160, 24), 13, TextAnchor.MiddleLeft, new Color(0.7f, 0.75f, 0.8f));
            var bat = RuntimeUiFactory.CreateLabel(hudGo.transform, "Battery", "Bat —", new Vector2(-120, -18),
                new Vector2(120, 24), 13, TextAnchor.MiddleLeft, new Color(0.7f, 0.75f, 0.8f));
            var warn = RuntimeUiFactory.CreateLabel(hudGo.transform, "Warning", "", new Vector2(40, -22),
                new Vector2(640, 28), 18, TextAnchor.MiddleLeft, new Color(1f, 0.55f, 0.4f));
            warn.horizontalOverflow = HorizontalWrapMode.Wrap;
            var lockLbl = RuntimeUiFactory.CreateLabel(hudGo.transform, "LockState", "REF BYPASS", new Vector2(340, 18),
                new Vector2(160, 28), 16, TextAnchor.MiddleRight, new Color(0.45f, 0.9f, 1f));
            hud.BindTexts(res, fps, usb, lat, fmt, bat, warn, lockLbl);
            hud.BindFreeze(freeze);
            hud.BindImageParams(imgCtrl);
            var hudCg = hudGo.GetComponent<CanvasGroup>() ?? hudGo.AddComponent<CanvasGroup>();
            hud.BindCanvasGroup(hudCg);

            // Systems
            var layoutGo = EnsureChild(root, "LayoutStore");
            var layout = layoutGo.GetComponent<LayoutStore>() ?? layoutGo.AddComponent<LayoutStore>();

            var appGo = EnsureChild(root, "CineQuestApp");
            // Avoid duplicate app if Editor menu already placed one on bootstrap.
            var existingApps = FindObjectsByType<CineQuestApp>(FindObjectsSortMode.None);
            CineQuestApp app;
            if (existingApps != null && existingApps.Length > 0)
            {
                app = existingApps[0];
                if (app.gameObject != appGo)
                {
                    // Keep single instance; destroy empty placeholder
                    Destroy(appGo);
                }
            }
            else
            {
                app = appGo.GetComponent<CineQuestApp>() ?? appGo.AddComponent<CineQuestApp>();
            }

            menu.Bind(imgCtrl, scopeMgr, theater, freeze, capture, layout, hud, panel, fc);
            menu.BindMenuCanvas(cg);
            menu.BindSliders(brS, brT, ctS, ctT, gmS, gmT, satS, satT, tmpS, tmpT, tintS, tintT, liftS, liftT);
            app.Bind(capture, imgCtrl, layout, menu, scopeMgr, theater);

            var inputGo = EnsureChild(root, "XrInputActions");
            var input = inputGo.GetComponent<XrInputActions>() ?? inputGo.AddComponent<XrInputActions>();
            input.Bind(menu, imgCtrl, theater, freeze, hud);

            var audioGo = EnsureChild(root, "CaptureAudio");
            audioGo.GetComponent<AudioSource>() ?? audioGo.AddComponent<AudioSource>();
            audioGo.GetComponent<CaptureAudioPlayer>() ?? audioGo.AddComponent<CaptureAudioPlayer>();

            var diagGo = EnsureChild(root, "Diagnostics");
            var fidelity = diagGo.GetComponent<FidelityDiagnostics>() ?? diagGo.AddComponent<FidelityDiagnostics>();
            fidelity.Bind(imgCtrl);
            var pulse = diagGo.GetComponent<SignalPulseTest>() ?? diagGo.AddComponent<SignalPulseTest>();
            pulse.Bind(capture);

            var voiceGo = EnsureChild(root, "VoiceStub");
            voiceGo.GetComponent<VoiceCommandStub>() ?? voiceGo.AddComponent<VoiceCommandStub>();

            if (createEventSystem && FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            _ = statusOverlay;

            Debug.Log("[CineQuest] RuntimeSceneBuilder complete. Editor: synthetic bars. Device: import UVC plugin + Meta XR ray UI.");
        }

        static VideoStatusOverlay BuildVideoStatusOverlay(GameObject panelGo, CaptureService capture)
        {
            var go = EnsureChild(panelGo, "StatusOverlay");
            go.transform.localPosition = new Vector3(0, 0, -0.002f);
            // Face the user after parent panel Y-180 (world canvas faces −Z).
            go.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            go.transform.localScale = Vector3.one * 0.001f;

            var canvas = go.GetComponent<Canvas>() ?? go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1600, 900);

            var group = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var bgGo = EnsureChild(go, "Bg");
            var bg = bgGo.GetComponent<Image>() ?? bgGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.07f, 0.9f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var msg = RuntimeUiFactory.CreateLabel(go.transform, "Message", "",
                Vector2.zero, new Vector2(1400, 400), 56, TextAnchor.MiddleCenter, Color.white);
            msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            msg.verticalOverflow = VerticalWrapMode.Overflow;

            var overlay = go.GetComponent<VideoStatusOverlay>() ?? go.AddComponent<VideoStatusOverlay>();
            overlay.Bind(capture, msg, bg, group);
            return overlay;
        }

        static GameObject CreateScopePanel(GameObject parent, string name, Vector3 pos, ScopeType type)
        {
            var go = EnsureChild(parent, name);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.8f, 0.5f, 1f);
            var mf = go.GetComponent<MeshFilter>() ?? go.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateQuad();
            var mr = go.GetComponent<MeshRenderer>() ?? go.AddComponent<MeshRenderer>();
            var sh = type == ScopeType.Vectorscope
                ? (Shader.Find("CineQuest/ScopeVectorscopeViz") ?? Shader.Find("CineQuest/ScopeWaveformViz"))
                : Shader.Find("CineQuest/ScopeWaveformViz");
            sh ??= Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (sh != null) mr.sharedMaterial = new Material(sh);
            var panel = go.GetComponent<ScopePanel>() ?? go.AddComponent<ScopePanel>();
            panel.SetType(type);
            go.GetComponent<SimpleGrabTransform>() ?? go.AddComponent<SimpleGrabTransform>();
            var box = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.02f);
            return go;
        }

        static GameObject EnsureChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static Mesh CreateQuad()
        {
            var mesh = new Mesh { name = "CQ_Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0)
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            return mesh;
        }

        static Mesh CreateCubeMesh()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
            if (Application.isPlaying) Object.Destroy(temp);
            else Object.DestroyImmediate(temp);
            return mesh;
        }
    }
}
