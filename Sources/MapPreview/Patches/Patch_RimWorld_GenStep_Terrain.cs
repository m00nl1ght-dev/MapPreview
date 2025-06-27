
#if !RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class Patch_RimWorld_GenStep_Terrain
{
    internal static bool SkipRiverFlowCalc = false;

    [HarmonyPrefix]
    [HarmonyPatch("GenerateRiverLookupTexture")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool GenerateRiverLookupTexture()
    {
        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        return !SkipRiverFlowCalc;
    }
}

#endif
