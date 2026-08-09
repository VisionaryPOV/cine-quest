// Cine Quest — Built-in image presets (thin Unity wrapper over Core.PresetCatalog).

using System.Collections.Generic;
using CineQuest.Core;
using CineQuest.Video;
using UnityEngine;

namespace CineQuest.Persistence
{
    public static class PresetLibrary
    {
        public const string NeutralLock = PresetCatalog.NeutralLock;
        public const string IrisEvaluation = PresetCatalog.IrisEvaluation;
        public const string LightingBalance = PresetCatalog.LightingBalance;
        public const string SkinToneCheck = PresetCatalog.SkinToneCheck;
        public const string ReferenceBypass = PresetCatalog.ReferenceBypass;

        public static IReadOnlyList<string> AllNames => PresetCatalog.AllNames;

        public static ImageParameters Get(string name)
        {
            return ImageParameters.FromState(PresetCatalog.Get(name));
        }

        /// <summary>Optional JSON presets under Resources/Presets/.</summary>
        public static ImageParameters LoadFromResources(string resourceName)
        {
            var ta = Resources.Load<TextAsset>($"Presets/{resourceName}");
            if (ta == null) return null;
            try
            {
                return JsonUtility.FromJson<ImageParameters>(ta.text);
            }
            catch
            {
                return null;
            }
        }
    }
}
