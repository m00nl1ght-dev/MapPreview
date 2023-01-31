using HarmonyLib;
using LunarFramework.Patching;
using Verse;

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
