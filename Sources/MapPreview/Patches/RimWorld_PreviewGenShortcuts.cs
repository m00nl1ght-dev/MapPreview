using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

/// <summary>
/// Skips certain steps in map generation when it's a preview map.
/// All patches are conditional and are only active while a preview is actually generating.
/// </summary>
[HarmonyPatch]
internal static class RimWorld_PreviewGenShortcuts
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GenStep_Terrain), "GenerateRiverLookupTexture")]
    private static bool GenStep_Terrain_GenerateRiverLookupTexture()
    {
        return !Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RiverMaker), "ValidatePassage")]
    private static bool RiverMaker_ValidatePassage()
    {
        return !Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TerrainGrid), "DoTerrainChangedEffects")]
    private static bool TerrainGrid_DoTerrainChangedEffects()
    {
        return !Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RegionAndRoomUpdater), "RegenerateNewRegionsFromDirtyCells")]
    private static bool RegionAndRoomUpdater_RegenerateNewRegionsFromDirtyCells()
    {
        return !Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(District), "Map", MethodType.Getter)]
    private static bool District_Map(ref Map __result)
    {
        if (!Main.IsGeneratingPreview) return true;
        var map = ExactMapPreviewGenerator.GeneratingMapOnCurrentThread;
        if (map == null) return true;
        __result = map;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Region), "Map", MethodType.Getter)]
    private static bool Region_Map(ref Map __result)
    {
        if (!Main.IsGeneratingPreview) return true;
        var map = ExactMapPreviewGenerator.GeneratingMapOnCurrentThread;
        if (map == null) return true;
        __result = map;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Root), "Shutdown")]
    private static void Root_Shutdown()
    {
        MapPreviewWindow.Dispose();
    }
}