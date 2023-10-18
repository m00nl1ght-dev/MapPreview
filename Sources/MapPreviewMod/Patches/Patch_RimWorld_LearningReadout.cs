using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace MapPreview.Patches;

[PatchGroup("Active")]
[HarmonyPatch(typeof(LearningReadout))]
internal static class Patch_RimWorld_LearningReadout
{
    [HarmonyPrefix]
    [HarmonyPatch("LearningReadoutOnGUI")]
    [HarmonyPriority(Priority.High)]
    private static bool LearningReadoutOnGUI()
    {
        return !MapSeedRerollWindow.IsOpen;
    }
}
