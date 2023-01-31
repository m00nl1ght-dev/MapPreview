using HarmonyLib;
using LunarFramework.Patching;
using Verse;

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
