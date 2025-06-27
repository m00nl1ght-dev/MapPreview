using HarmonyLib;
using LunarFramework.Patching;
using Verse;

#if RW_1_6_OR_GREATER
using RimWorld.Planet;
#endif

namespace MapPreview.Patches;

[PatchGroup("GenLow")]
[HarmonyPatch(typeof(GenSpawn))]
internal static class Patch_Verse_GenSpawn
{
    #if RW_1_6_OR_GREATER
    private static PlanetTile _lastWarnedTile = PlanetTile.Invalid;
    #else
    private static int _lastWarnedTile = -1;
    #endif

    [HarmonyPrefix]
    #if RW_1_6_OR_GREATER
    [HarmonyPatch("Spawn", typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode))]
    #else
    [HarmonyPatch("Spawn", typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode))]
    #endif
    [PatchExcludedFromConflictCheck]
    private static bool Spawn_ByDef_Prefix(ThingDef def, ref Thing __result)
    {
        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;

        __result = null;
        return false;
    }

    [HarmonyPrefix]
    #if RW_1_5_OR_GREATER
    [HarmonyPatch("Spawn", typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool))]
    #else
    [HarmonyPatch("Spawn", typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool))]
    #endif
    [PatchExcludedFromConflictCheck]
    private static bool Spawn_Prefix(Thing newThing, Map map, ref Thing __result)
    {
        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;

        if (_lastWarnedTile != map.Tile)
            MapPreviewAPI.Logger.Warn($"Attempted to spawn thing {newThing} on a preview map {map.Tile}, this is not supported!");

        _lastWarnedTile = map.Tile;

        __result = null;
        return false;
    }
}
