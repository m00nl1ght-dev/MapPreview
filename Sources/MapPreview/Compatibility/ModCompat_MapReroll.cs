using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using MapPreview.Patches;
using UnityEngine;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_MapReroll : ModCompat
{
    public override string TargetAssembly => "MapReroll";
    public override string DisplayName => "Map Reroll";
    
    private static bool _trueTerrainColorsApplied;

    [HarmonyPrefix]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "GeneratePreviewForSeed")]
    private static void MapPreviewGenerator_GeneratePreviewForSeed_Prefix(Dictionary<string, Color> ___terrainColors)
    {
        Main.IsGeneratingPreview = true;
        UpdateTerrainColorsIfNeeded(___terrainColors);
        Patch_RimWorld_TerrainPatchMaker.Reset();
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "GeneratePreviewForSeed")]
    private static void MapPreviewGenerator_GeneratePreviewForSeed_Postfix()
    {
        Main.IsGeneratingPreview = false;
    }
    
    private static void UpdateTerrainColorsIfNeeded(Dictionary<string, Color> terrainColors)
    {
        var enabled = TrueTerrainColors.EnabledFunc.Invoke();
        if (enabled != _trueTerrainColorsApplied)
        {
            terrainColors.Clear();
            var activeColors = TrueTerrainColors.ActiveColors;
            foreach (var pair in activeColors) terrainColors.Add(pair.Key, pair.Value);
            _trueTerrainColorsApplied = enabled;
        }
    }
}