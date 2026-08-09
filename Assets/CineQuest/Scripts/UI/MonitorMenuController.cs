// Cine Quest — Wrist / palm settings menu: lock, params, scopes, presets, theater.

using CineQuest.Capture;
using CineQuest.Persistence;
using CineQuest.Scopes;
using CineQuest.Video;
using UnityEngine;
using UnityEngine.UI;

namespace CineQuest.UI
{
    /// <summary>
    /// Binds UI controls to image parameters and app features.
    /// Wire sliders/toggles in the Inspector or via RuntimeSceneBuilder.
    /// </summary>
    public sealed class MonitorMenuController : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] ImageParameterController imageParams;
        [SerializeField] ScopeManager scopeManager;
        [SerializeField] TheaterModeController theater;
        [SerializeField] FreezeFrameController freezeFrame;
        [SerializeField] CaptureService captureService;
        [SerializeField] LayoutStore layoutStore;
        [SerializeField] StatusHud statusHud;
        [SerializeField] VideoPanel videoPanel;
        [SerializeField] FalseColorController falseColor;

        [Header("Toggles")]
        [SerializeField] Toggle lockToggle;
        [SerializeField] Toggle bypassToggle;
        [SerializeField] Toggle limitedRangeToggle;
        [SerializeField] Toggle waveformToggle;
        [SerializeField] Toggle paradeToggle;
        [SerializeField] Toggle vectorscopeToggle;
        [SerializeField] Toggle histogramToggle;
        [SerializeField] Toggle theaterToggle;
        [SerializeField] Toggle freezeToggle;
        [SerializeField] Toggle falseColorToggle;

        [Header("Sliders")]
        [SerializeField] Slider brightnessSlider;
        [SerializeField] Slider contrastSlider;
        [SerializeField] Slider gammaSlider;
        [SerializeField] Slider saturationSlider;
        [SerializeField] Slider temperatureSlider;
        [SerializeField] Slider tintSlider;
        [SerializeField] Slider liftSlider;
        [SerializeField] Slider opacitySlider;

        [Header("Readouts")]
        [SerializeField] Text brightnessValue;
        [SerializeField] Text contrastValue;
        [SerializeField] Text gammaValue;
        [SerializeField] Text saturationValue;
        [SerializeField] Text temperatureValue;
        [SerializeField] Text tintValue;
        [SerializeField] Text liftValue;

        [Header("Presets")]
        [SerializeField] Dropdown presetDropdown;

        [Header("Visibility")]
        [SerializeField] CanvasGroup menuCanvas;
        [SerializeField] bool startVisible = true;

        bool _suppressUi;

        void Start()
        {
            AutoBind();
            WireUi();
            PullFromModel();
            SetMenuVisible(startVisible);

            if (imageParams != null)
                imageParams.ParametersChanged += _ => PullFromModel();
        }

        public void Bind(
            ImageParameterController img,
            ScopeManager scopes,
            TheaterModeController th,
            FreezeFrameController fr,
            CaptureService cap,
            LayoutStore store,
            StatusHud hud,
            VideoPanel panel,
            FalseColorController fc)
        {
            if (img != null) imageParams = img;
            if (scopes != null) scopeManager = scopes;
            if (th != null) theater = th;
            if (fr != null) freezeFrame = fr;
            if (cap != null) captureService = cap;
            if (store != null) layoutStore = store;
            if (hud != null) statusHud = hud;
            if (panel != null) videoPanel = panel;
            if (fc != null) falseColor = fc;
        }

        void AutoBind()
        {
            if (imageParams == null) imageParams = FindFirstObjectByType<ImageParameterController>();
            if (scopeManager == null) scopeManager = FindFirstObjectByType<ScopeManager>();
            if (theater == null) theater = FindFirstObjectByType<TheaterModeController>();
            if (freezeFrame == null) freezeFrame = FindFirstObjectByType<FreezeFrameController>();
            if (captureService == null) captureService = CaptureService.Instance ?? FindFirstObjectByType<CaptureService>();
            if (layoutStore == null) layoutStore = FindFirstObjectByType<LayoutStore>();
            if (statusHud == null) statusHud = FindFirstObjectByType<StatusHud>();
            if (videoPanel == null) videoPanel = FindFirstObjectByType<VideoPanel>();
            if (falseColor == null) falseColor = FindFirstObjectByType<FalseColorController>();
        }

        void WireUi()
        {
            if (lockToggle) lockToggle.onValueChanged.AddListener(v =>
            {
                if (_suppressUi) return;
                imageParams?.SetLocked(v);
                statusHud?.SetLockLabel(imageParams != null && imageParams.IsLocked,
                    imageParams != null && imageParams.IsBypass);
            });

            if (bypassToggle) bypassToggle.onValueChanged.AddListener(v =>
            {
                if (_suppressUi) return;
                imageParams?.SetBypass(v);
                statusHud?.SetLockLabel(imageParams != null && imageParams.IsLocked,
                    imageParams != null && imageParams.IsBypass);
            });

            if (limitedRangeToggle) limitedRangeToggle.onValueChanged.AddListener(v =>
            {
                if (_suppressUi) return;
                imageParams?.SetColorSpace(v ? VideoColorSpace.Rec709Limited : VideoColorSpace.FullRange);
            });

            WireSlider(brightnessSlider, brightnessValue, "brightness", v => v.ToString("0.00"));
            WireSlider(contrastSlider, contrastValue, "contrast", v => v.ToString("0.00"));
            WireSlider(gammaSlider, gammaValue, "gamma", v => v.ToString("0.00"));
            WireSlider(saturationSlider, saturationValue, "saturation", v => v.ToString("0.00"));
            WireSlider(temperatureSlider, temperatureValue, "temperature", v => v.ToString("0.00"));
            WireSlider(tintSlider, tintValue, "tint", v => v.ToString("0.00"));
            WireSlider(liftSlider, liftValue, "lift", v => v.ToString("0.00"));

            if (waveformToggle) waveformToggle.onValueChanged.AddListener(v =>
            {
                if (!_suppressUi) scopeManager?.SetScopeEnabled(ScopeType.Waveform, v);
            });
            if (paradeToggle) paradeToggle.onValueChanged.AddListener(v =>
            {
                if (!_suppressUi) scopeManager?.SetScopeEnabled(ScopeType.RgbParade, v);
            });
            if (vectorscopeToggle) vectorscopeToggle.onValueChanged.AddListener(v =>
            {
                if (!_suppressUi) scopeManager?.SetScopeEnabled(ScopeType.Vectorscope, v);
            });
            if (histogramToggle) histogramToggle.onValueChanged.AddListener(v =>
            {
                if (!_suppressUi) scopeManager?.SetScopeEnabled(ScopeType.Histogram, v);
            });

            if (theaterToggle) theaterToggle.onValueChanged.AddListener(v =>
            {
                if (_suppressUi) return;
                theater?.SetMode(v ? EnvironmentMode.Theater : EnvironmentMode.Passthrough);
            });

            if (freezeToggle) freezeToggle.onValueChanged.AddListener(v =>
            {
                if (_suppressUi) return;
                if (v) freezeFrame?.Freeze(); else freezeFrame?.Unfreeze();
            });

            if (falseColorToggle) falseColorToggle.onValueChanged.AddListener(v =>
            {
                if (!_suppressUi) falseColor?.SetEnabled(v);
            });

            if (presetDropdown != null)
            {
                presetDropdown.ClearOptions();
                presetDropdown.AddOptions(new System.Collections.Generic.List<string>(PresetLibrary.AllNames));
                presetDropdown.onValueChanged.AddListener(i =>
                {
                    if (_suppressUi) return;
                    var name = PresetLibrary.AllNames[Mathf.Clamp(i, 0, PresetLibrary.AllNames.Count - 1)];
                    ApplyPreset(name);
                });
            }
        }

        void WireSlider(Slider slider, Text readout, string param, System.Func<float, string> fmt)
        {
            if (slider == null) return;
            slider.onValueChanged.AddListener(v =>
            {
                if (_suppressUi) return;
                imageParams?.TrySet(param, v);
                if (readout) readout.text = fmt(v);
            });
        }

        public void ApplyPreset(string name)
        {
            var p = PresetLibrary.Get(name);
            imageParams?.ApplyPreset(p, forceUnlock: true);
            PullFromModel();
        }

        public void SaveLayout()
        {
            if (layoutStore == null || imageParams == null) return;
            var data = new LayoutData
            {
                name = "User",
                image = imageParams.Parameters.Clone(),
                environment = theater != null && theater.Mode == EnvironmentMode.Theater ? "Theater" : "Passthrough",
                qualityMode = scopeManager != null ? scopeManager.QualityMode.ToString() : "Balanced"
            };
            if (videoPanel != null)
            {
                data.mainPanel = new PanelPoseData
                {
                    id = "main",
                    position = videoPanel.transform.position,
                    rotation = videoPanel.transform.rotation,
                    scale = videoPanel.transform.localScale
                };
            }
            layoutStore.Save(data);
        }

        public void LoadLayout()
        {
            if (layoutStore == null || !layoutStore.TryLoad(out var data) || data == null) return;
            if (data.image != null)
                imageParams?.ReplaceAll(data.image);
            if (data.mainPanel != null && videoPanel != null)
                videoPanel.SetPose(data.mainPanel.position, data.mainPanel.rotation, data.mainPanel.scale);
            if (theater != null)
                theater.SetMode(data.environment == "Theater" ? EnvironmentMode.Theater : EnvironmentMode.Passthrough);
            PullFromModel();
        }

        public void ToggleMenu()
        {
            bool vis = menuCanvas == null || menuCanvas.alpha > 0.5f;
            SetMenuVisible(!vis);
        }

        public void SetMenuVisible(bool visible)
        {
            if (menuCanvas != null)
            {
                menuCanvas.alpha = visible ? 1f : 0f;
                menuCanvas.blocksRaycasts = visible;
                menuCanvas.interactable = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetQuality(int mode)
        {
            if (scopeManager != null)
                scopeManager.QualityMode = (ScopeQualityMode)Mathf.Clamp(mode, 0, 2);
        }

        public void SetSyntheticPattern(int pattern)
        {
            captureService?.SetSyntheticPattern((SyntheticPattern)pattern);
        }

        void PullFromModel()
        {
            if (imageParams == null || imageParams.Parameters == null) return;
            var p = imageParams.Parameters;
            _suppressUi = true;

            if (lockToggle) lockToggle.isOn = p.locked;
            if (bypassToggle) bypassToggle.isOn = p.bypass;
            if (limitedRangeToggle) limitedRangeToggle.isOn = p.colorSpace == VideoColorSpace.Rec709Limited;

            SetSlider(brightnessSlider, brightnessValue, p.brightness, "0.00");
            SetSlider(contrastSlider, contrastValue, p.contrast, "0.00");
            SetSlider(gammaSlider, gammaValue, p.gamma, "0.00");
            SetSlider(saturationSlider, saturationValue, p.saturation, "0.00");
            SetSlider(temperatureSlider, temperatureValue, p.temperature, "0.00");
            SetSlider(tintSlider, tintValue, p.tint, "0.00");
            SetSlider(liftSlider, liftValue, p.lift, "0.00");

            bool locked = p.locked;
            SetInteractable(brightnessSlider, !locked);
            SetInteractable(contrastSlider, !locked);
            SetInteractable(gammaSlider, !locked);
            SetInteractable(saturationSlider, !locked);
            SetInteractable(temperatureSlider, !locked);
            SetInteractable(tintSlider, !locked);
            SetInteractable(liftSlider, !locked);

            statusHud?.SetLockLabel(p.locked, p.bypass);
            _suppressUi = false;
        }

        static void SetSlider(Slider s, Text t, float v, string fmt)
        {
            if (s) s.SetValueWithoutNotify(v);
            if (t) t.text = v.ToString(fmt);
        }

        static void SetInteractable(Slider s, bool on)
        {
            if (s) s.interactable = on;
        }
    }
}
