using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(RiverMaker))]
internal static class Patch_RimWorld_RiverMaker
{
    [HarmonyPrefix]
    [HarmonyPatch("ValidatePassage")]
    private static bool ValidatePassage()
    {
        return !Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}