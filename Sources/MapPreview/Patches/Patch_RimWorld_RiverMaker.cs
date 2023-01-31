using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(RiverMaker))]
internal static class Patch_RimWorld_RiverMaker
{
    [HarmonyPrefix]
    [HarmonyPatch("ValidatePassage")]
    private static bool ValidatePassage()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
