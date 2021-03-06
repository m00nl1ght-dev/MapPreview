using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[HarmonyPatch]
internal static class RimWorld_PreviewGenShortcuts
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GenStep_Terrain), "GenerateRiverLookupTexture")]
    private static bool GenStep_Terrain_GenerateRiverLookupTexture()
    {
        return !Main.IsGeneratingPreview;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RiverMaker), "ValidatePassage")]
    private static bool RiverMaker_ValidatePassage()
    {
        return !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainGrid), "DoTerrainChangedEffects")]
    private static bool TerrainGrid_DoTerrainChangedEffects()
    {
        return !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RegionAndRoomUpdater), "TryRebuildDirtyRegionsAndRooms")]
    private static bool RegionAndRoomUpdater_TryRebuildDirtyRegionsAndRooms()
    {
        return !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(District), "Map", MethodType.Getter)]
    private static bool District_Map(ref Map __result)
    {
        if (!ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        var map = ExactMapPreviewGenerator.GeneratingMapOnCurrentThread;
        if (map == null) return true;
        __result = map;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Region), "Map", MethodType.Getter)]
    private static bool Region_Map(ref Map __result)
    {
        if (!ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        var map = ExactMapPreviewGenerator.GeneratingMapOnCurrentThread;
        if (map == null) return true;
        __result = map;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TickManager), "TicksAbs", MethodType.Getter)]
    private static bool TickManager_TicksAbs(ref int __result)
    {
        if (!ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __result = 0;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Root), "Shutdown")]
    private static void Root_Shutdown()
    {
        MapPreviewWindow.Dispose();
    }
}