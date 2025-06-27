
#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(TileMutatorWorker_River))]
internal static class Patch_RimWorld_TileMutatorWorker_River
{
    [HarmonyPrefix]
    [HarmonyPatch("GenerateRiverLookupTexture")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool GenerateRiverLookupTexture()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}

#endif
