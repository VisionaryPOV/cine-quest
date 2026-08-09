// Cine Quest — Built-in presets as pure data (shipped; used by Unity PresetLibrary).

using System.Collections.Generic;

namespace CineQuest.Core
{
    public static class PresetCatalog
    {
        public const string NeutralLock = "Neutral Lock";
        public const string IrisEvaluation = "Iris Evaluation";
        public const string LightingBalance = "Lighting Balance";
        public const string SkinToneCheck = "Skin Tone Check";
        public const string ReferenceBypass = "Reference Bypass";

        public static IReadOnlyList<string> AllNames { get; } = new[]
        {
            NeutralLock,
            IrisEvaluation,
            LightingBalance,
            SkinToneCheck,
            ReferenceBypass
        };

        public static ImageParameterState Get(string name)
        {
            switch (name)
            {
                case IrisEvaluation:
                    return new ImageParameterState
                    {
                        locked = true,
                        bypass = false,
                        colorSpace = ColorSpaceMode.Rec709Limited,
                        brightness = 0f,
                        contrast = 1.05f,
                        gamma = 1f,
                        saturation = 0.85f,
                        temperature = 0f,
                        tint = 0f,
                        lift = 0f
                    };

                case LightingBalance:
                    return new ImageParameterState
                    {
                        locked = true,
                        bypass = false,
                        colorSpace = ColorSpaceMode.Rec709Limited,
                        brightness = 0f,
                        contrast = 1.1f,
                        gamma = 1f,
                        saturation = 1.15f,
                        temperature = 0f,
                        tint = 0f,
                        lift = 0.02f
                    };

                case SkinToneCheck:
                    return new ImageParameterState
                    {
                        locked = true,
                        bypass = false,
                        colorSpace = ColorSpaceMode.Rec709Limited,
                        brightness = 0f,
                        contrast = 1f,
                        gamma = 1f,
                        saturation = 1.05f,
                        temperature = 0.05f,
                        tint = 0f,
                        lift = 0f
                    };

                case ReferenceBypass:
                    return ImageParameterState.CreateBypass();

                case NeutralLock:
                default:
                    var n = ImageParameterState.CreateNeutral();
                    n.locked = true;
                    return n;
            }
        }
    }
}
