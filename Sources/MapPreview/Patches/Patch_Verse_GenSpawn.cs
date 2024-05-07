using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("GenLow")]
[HarmonyPatch(typeof(GenSpawn))]
internal static class Patch_Verse_GenSpawn
{
    [HarmonyPrefix]
    #if RW_1_5_OR_GREATER
    [HarmonyPatch("Spawn", typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool))]
    #else
    [HarmonyPatch("Spawn", typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool))]
    #endif
    [PatchExcludedFromConflictCheck]
    private static bool Spawn(Thing newThing, Map map, ref Thing __result)
    {
        if (!MapPreviewAPI.IsGeneratingPreview) return true;
        var generating = MapPreviewGenerator.GeneratingMapOnCurrentThread;
        if (generating != map) return true;

        MapPreviewAPI.Logger.Warn("Some mod attempted to spawn thing " + newThing + " on a preview map, this is not supported!");

        __result = null;
        return false;
    }
}
