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
[HarmonyPatch(typeof(District))]
internal static class Patch_Verse_District
{
    [HarmonyPrefix]
    [HarmonyPatch("Map", MethodType.Getter)]
    private static bool Map(ref Map __result)
    {
        if (!MapPreviewAPI.IsGeneratingPreview) return true;
        var map = MapPreviewGenerator.GeneratingMapOnCurrentThread;
        if (map == null) return true;
        __result = map;
        return false;
    }
}