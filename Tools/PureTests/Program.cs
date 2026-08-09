// Cine Quest — headless pure-logic tests against shipped Core sources.
// Run: dotnet run --project Tools/PureTests
// Exercises ImageParameterState, PresetCatalog, LayoutSerializer, ScopeQualityPolicy.

using System;
using System.Collections.Generic;
using System.IO;
using CineQuest.Core;

static class Program
{
    static int _passed;
    static int _failed;
    static readonly List<string> Failures = new();

    static int Main(string[] args)
    {
        Console.WriteLine("=== Cine Quest Pure Logic Tests ===");
        Console.WriteLine($"UTC: {DateTime.UtcNow:o}");
        Console.WriteLine();

        Test_NeutralDefaults();
        Test_LockBlocksTrySet();
        Test_UnlockAllowsTrySet();
        Test_BypassForcesIdentityEffectiveGrade();
        Test_BypassSetsLocked();
        Test_ColorSpaceAllowedWhenLocked();
        Test_ContrastFormula();
        Test_BypassCreativeIsIdentity();
        Test_LimitedExpand();
        Test_Presets();
        Test_LayoutRoundTrip();
        Test_ScopeQualityPolicy();
        Test_ApplyPresetRespectsLock();

        Console.WriteLine();
        Console.WriteLine($"Result: {_passed} passed, {_failed} failed");
        if (_failed > 0)
        {
            Console.WriteLine("Failures:");
            foreach (var f in Failures) Console.WriteLine("  - " + f);
            return 1;
        }
        Console.WriteLine("ALL PASSED");
        return 0;
    }

    static void Test_NeutralDefaults()
    {
        var p = ImageParameterState.CreateNeutral();
        Assert(!p.locked, "neutral unlocked");
        Assert(!p.bypass, "neutral not bypass");
        Assert(p.colorSpace == ColorSpaceMode.Rec709Limited, "neutral Rec.709 limited");
        Assert(Near(p.contrast, 1f), "neutral contrast 1");
        Assert(Near(p.gamma, 1f), "neutral gamma 1");
        Assert(Near(p.saturation, 1f), "neutral sat 1");
    }

    static void Test_LockBlocksTrySet()
    {
        var p = ImageParameterState.CreateNeutral();
        p.SetLocked(true);
        Assert(!p.TrySet("brightness", 0.5f), "locked rejects brightness");
        Assert(Near(p.brightness, 0f), "brightness unchanged when locked");
        Assert(!p.TrySet("contrast", 1.5f), "locked rejects contrast");
    }

    static void Test_UnlockAllowsTrySet()
    {
        var p = ImageParameterState.CreateNeutral();
        p.SetLocked(false);
        Assert(p.TrySet("brightness", 0.25f), "unlocked accepts brightness");
        Assert(Near(p.brightness, 0.25f), "brightness stored");
        Assert(p.TrySet("contrast", 1.2f), "unlocked accepts contrast");
        Assert(Near(p.contrast, 1.2f), "contrast stored");
        Assert(p.TrySet("lift", 0.1f), "unlocked accepts lift");
        Assert(Near(p.lift, 0.1f), "lift stored");
    }

    static void Test_BypassForcesIdentityEffectiveGrade()
    {
        var p = ImageParameterState.CreateNeutral();
        p.TrySet("brightness", 0.4f);
        p.TrySet("contrast", 1.5f);
        p.TrySet("gamma", 1.2f);
        p.TrySet("saturation", 0.5f);
        p.TrySet("temperature", 0.3f);
        p.TrySet("tint", -0.2f);
        p.TrySet("lift", 0.1f);
        p.SetBypass(true);

        var g = p.GetEffectiveGrade();
        Assert(g.Bypass, "effective bypass true");
        Assert(Near(g.Brightness, 0f), "bypass brightness 0");
        Assert(Near(g.Contrast, 1f), "bypass contrast 1");
        Assert(Near(g.Gamma, 1f), "bypass gamma 1");
        Assert(Near(g.Saturation, 1f), "bypass sat 1");
        Assert(Near(g.Temperature, 0f), "bypass temp 0");
        Assert(Near(g.Tint, 0f), "bypass tint 0");
        Assert(Near(g.Lift, 0f), "bypass lift 0");
        Assert(g.LimitedRange, "bypass keeps limited flag from color space");
    }

    static void Test_BypassSetsLocked()
    {
        var p = ImageParameterState.CreateNeutral();
        p.SetBypass(true);
        Assert(p.locked, "bypass implies locked");
        Assert(p.bypass, "bypass flag set");
    }

    static void Test_ColorSpaceAllowedWhenLocked()
    {
        var p = ImageParameterState.CreateNeutral();
        p.SetLocked(true);
        p.SetColorSpace(ColorSpaceMode.FullRange);
        Assert(p.colorSpace == ColorSpaceMode.FullRange, "color space changes while locked");
        var g = p.GetEffectiveGrade();
        Assert(!g.LimitedRange, "full range clears limited flag");
    }

    static void Test_ContrastFormula()
    {
        // Shipped formula: (c - 0.5) * contrast + 0.5
        float r = ImageParameterState.ApplyContrast(0.25f, 2f);
        Assert(Near(r, 0.0f), "contrast 2 maps 0.25 → 0");
        r = ImageParameterState.ApplyContrast(0.75f, 2f);
        Assert(Near(r, 1.0f), "contrast 2 maps 0.75 → 1");
        r = ImageParameterState.ApplyContrast(0.5f, 1.7f);
        Assert(Near(r, 0.5f), "mid gray fixed point");
    }

    static void Test_BypassCreativeIsIdentity()
    {
        var p = ImageParameterState.CreateBypass();
        float input = 0.37f;
        float outv = p.ApplyCreativeToChannel(input);
        Assert(Near(outv, input), "bypass creative is identity");
    }

    static void Test_LimitedExpand()
    {
        // 16/255 → ~0
        float black = ImageParameterState.ExpandLimited(16f / 255f);
        Assert(Near(black, 0f), "limited black expands to 0");
        float white = ImageParameterState.ExpandLimited(235f / 255f);
        Assert(Near(white, 1f), "limited white expands to 1");
    }

    static void Test_Presets()
    {
        Assert(PresetCatalog.AllNames.Count == 5, "five presets");
        var names = new HashSet<string>(PresetCatalog.AllNames);
        Assert(names.Contains(PresetCatalog.NeutralLock), "has Neutral Lock");
        Assert(names.Contains(PresetCatalog.IrisEvaluation), "has Iris Evaluation");
        Assert(names.Contains(PresetCatalog.LightingBalance), "has Lighting Balance");
        Assert(names.Contains(PresetCatalog.SkinToneCheck), "has Skin Tone Check");
        Assert(names.Contains(PresetCatalog.ReferenceBypass), "has Reference Bypass");

        var neutral = PresetCatalog.Get(PresetCatalog.NeutralLock);
        Assert(neutral.locked && !neutral.bypass, "Neutral Lock locked not bypass");
        Assert(Near(neutral.contrast, 1f), "Neutral contrast 1");

        var iris = PresetCatalog.Get(PresetCatalog.IrisEvaluation);
        Assert(iris.locked && Near(iris.contrast, 1.05f), "Iris eval contrast");
        Assert(Near(iris.saturation, 0.85f), "Iris eval sat");

        var light = PresetCatalog.Get(PresetCatalog.LightingBalance);
        Assert(Near(light.contrast, 1.1f) && Near(light.saturation, 1.15f), "Lighting balance");

        var skin = PresetCatalog.Get(PresetCatalog.SkinToneCheck);
        Assert(Near(skin.temperature, 0.05f), "Skin tone temp");

        var bypass = PresetCatalog.Get(PresetCatalog.ReferenceBypass);
        Assert(bypass.bypass && bypass.locked, "Reference Bypass");
        var g = bypass.GetEffectiveGrade();
        Assert(Near(g.Contrast, 1f) && g.Bypass, "Reference Bypass effective identity");
    }

    static void Test_LayoutRoundTrip()
    {
        var doc = new LayoutDocument
        {
            version = 1,
            name = "UnitTestLayout",
            environment = "Theater",
            qualityMode = "High",
            falseColor = true,
            audioMuted = false,
            image = PresetCatalog.Get(PresetCatalog.IrisEvaluation),
            mainPanel = new PanelPose
            {
                id = "main",
                px = 1.1f, py = 1.4f, pz = 0.5f,
                qx = 0, qy = 0.707f, qz = 0, qw = 0.707f,
                sx = 1.2f, sy = 1.2f, sz = 1.2f
            },
            scopes = new List<ScopePose>
            {
                new ScopePose { type = "Waveform", enabled = true, opacity = 0.9f, px = -0.9f, py = 1.1f, pz = 1.4f },
                new ScopePose { type = "Vectorscope", enabled = false, opacity = 0.8f, px = 0, py = 0.55f, pz = 1.5f }
            }
        };

        string json = LayoutSerializer.Serialize(doc);
        Assert(json.Contains("UnitTestLayout"), "json contains name");
        Assert(json.Contains("Waveform"), "json contains scope type");

        var round = LayoutSerializer.Deserialize(json);
        Assert(round.name == "UnitTestLayout", "name round-trip");
        Assert(round.environment == "Theater", "env round-trip");
        Assert(round.qualityMode == "High", "quality round-trip");
        Assert(round.falseColor, "falseColor round-trip");
        Assert(!round.audioMuted, "audioMuted round-trip");
        Assert(round.image != null && round.image.locked, "image locked");
        Assert(Near(round.image.contrast, 1.05f), "image contrast round-trip");
        Assert(Near(round.mainPanel.px, 1.1f), "panel px");
        Assert(Near(round.mainPanel.sx, 1.2f), "panel scale");
        Assert(round.scopes.Count == 2, "two scopes");
        Assert(round.scopes[0].type == "Waveform" && round.scopes[0].enabled, "waveform scope");
        Assert(round.scopes[1].type == "Vectorscope" && !round.scopes[1].enabled, "vectorscope scope");
    }

    static void Test_ScopeQualityPolicy()
    {
        Assert(ScopeQualityPolicy.ShouldUpdate(ScopeQuality.High, 1, 0f), "high always");
        Assert(ScopeQualityPolicy.ShouldUpdate(ScopeQuality.Balanced, 2, 0f), "balanced even frame");
        Assert(!ScopeQualityPolicy.ShouldUpdate(ScopeQuality.Balanced, 3, 0f), "balanced odd frame skip");
        Assert(!ScopeQualityPolicy.ShouldUpdate(ScopeQuality.Performance, 10, 0.01f), "perf waits");
        Assert(ScopeQualityPolicy.ShouldUpdate(ScopeQuality.Performance, 10, 0.06f), "perf after 50ms+");
        Assert(ScopeQualityPolicy.AnalysisWidth(ScopeQuality.Balanced, 1920) == 640, "balanced width");
        Assert(ScopeQualityPolicy.AnalysisWidth(ScopeQuality.Performance, 1920) == 480, "perf width");
        Assert(ScopeQualityPolicy.AnalysisWidth(ScopeQuality.High, 800) == 800, "high clamps to source");
    }

    static void Test_ApplyPresetRespectsLock()
    {
        var p = ImageParameterState.CreateNeutral();
        p.SetLocked(true);
        var iris = PresetCatalog.Get(PresetCatalog.IrisEvaluation);
        p.ApplyPreset(iris, forceUnlock: false);
        Assert(Near(p.contrast, 1f), "locked blocks non-bypass preset");
        p.ApplyPreset(iris, forceUnlock: true);
        Assert(Near(p.contrast, 1.05f), "forceUnlock applies preset");
    }

    static void Assert(bool condition, string name)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine($"  PASS  {name}");
        }
        else
        {
            _failed++;
            Failures.Add(name);
            Console.WriteLine($"  FAIL  {name}");
        }
    }

    static bool Near(float a, float b, float eps = 1e-4f) => Math.Abs(a - b) <= eps;
}
