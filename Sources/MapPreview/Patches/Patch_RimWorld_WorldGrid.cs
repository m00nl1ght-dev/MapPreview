using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

/// <summary>
/// Holds a separate state for WorldGrid caches on the preview thread.
/// All patches are conditional and are only active while a preview is actually generating.
/// </summary>
[PatchGroup("Gen")]
[HarmonyPatch(typeof(WorldGrid))]
internal static class Patch_RimWorld_WorldGrid
{
    private static readonly List<int> tmpNeighbors = new();

    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.IsNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool IsNeighbor(int tile1, int tile2, ref bool __result, WorldGrid __instance)
    {
        if (!Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __instance.GetTileNeighbors(tile1, tmpNeighbors);
        __result = tmpNeighbors.Contains(tile2);
        return false;
    }
    
    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.GetNeighborId))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool GetNeighborId(int tile1, int tile2, ref int __result, WorldGrid __instance)
    {
        if (!Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __instance.GetTileNeighbors(tile1, tmpNeighbors);
        __result = tmpNeighbors.IndexOf(tile2);
        return false;
    }
    
    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.GetTileNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool GetTileNeighbor(int tileID, int adjacentId, ref int __result, WorldGrid __instance)
    {
        if (!Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __instance.GetTileNeighbors(tileID, tmpNeighbors);
        __result = tmpNeighbors[adjacentId];
        return false;
    }
}