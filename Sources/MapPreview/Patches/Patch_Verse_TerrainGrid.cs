using HarmonyLib;
using LunarFramework.Patching;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Gen")]
[HarmonyPatch(typeof(TerrainGrid))]
internal static class Patch_Verse_TerrainGrid
{
    [HarmonyPrefix]
    [HarmonyPatch("DoTerrainChangedEffects")]
    private static bool DoTerrainChangedEffects()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}