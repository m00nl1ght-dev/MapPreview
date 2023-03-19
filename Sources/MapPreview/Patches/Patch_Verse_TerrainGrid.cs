using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(TerrainGrid))]
internal static class Patch_Verse_TerrainGrid
{
    [HarmonyPrefix]
    [HarmonyPatch("DoTerrainChangedEffects")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool DoTerrainChangedEffects()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
