using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Gen")]
[HarmonyPatch(typeof(GenStep_Terrain))]
internal static class Patch_RimWorld_GenStep_Terrain
{
    [HarmonyPrefix]
    [HarmonyPatch("GenerateRiverLookupTexture")]
    private static bool GenerateRiverLookupTexture()
    {
        return !Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}