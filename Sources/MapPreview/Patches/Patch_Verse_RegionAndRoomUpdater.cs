using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Gen")]
[HarmonyPatch(typeof(RegionAndRoomUpdater))]
internal static class Patch_Verse_RegionAndRoomUpdater
{
    [HarmonyPrefix]
    [HarmonyPatch("RebuildAllRegionsAndRooms")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool RebuildAllRegionsAndRooms()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("RegenerateNewRegionsFromDirtyCells")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool RegenerateNewRegionsFromDirtyCells()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
