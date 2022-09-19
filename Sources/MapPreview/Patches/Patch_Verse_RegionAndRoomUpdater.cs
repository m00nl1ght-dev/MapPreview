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
[HarmonyPatch(typeof(RegionAndRoomUpdater))]
internal static class Patch_Verse_RegionAndRoomUpdater
{
    [HarmonyPrefix]
    [HarmonyPatch("RegenerateNewRegionsFromDirtyCells")]
    private static bool RegenerateNewRegionsFromDirtyCells()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}