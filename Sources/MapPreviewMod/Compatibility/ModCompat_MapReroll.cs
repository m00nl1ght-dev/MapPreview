using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using UnityEngine;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_MapReroll : ModCompat
{
    public static bool IsPresent { get; private set; }
    
    public override string TargetAssemblyName => "MapReroll";
    public override string DisplayName => "Map Reroll";
    
    private static bool _trueTerrainColorsApplied;

    protected override bool OnApply()
    {
        MapPreviewMod.Settings.EnableSeedRerollFeature = false;
        IsPresent = true;
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("MapReroll.MapPreviewGenerator", "GeneratePreviewForSeed")]
    private static void MapPreviewGenerator_GeneratePreviewForSeed_Prefix(Dictionary<string, Color> ___terrainColors)
    {
        UpdateTerrainColorsIfNeeded(___terrainColors);
    }
    
    private static void UpdateTerrainColorsIfNeeded(Dictionary<string, Color> terrainColors)
    {
        var enabled = MapPreviewMod.Settings.EnableTrueTerrainColors;
        if (enabled != _trueTerrainColorsApplied)
        {
            terrainColors.Clear();
            var activeColors = enabled ? TrueTerrainColors.TrueColors : TrueTerrainColors.DefaultColors;
            foreach (var pair in activeColors) terrainColors.Add(pair.Key, pair.Value);
            _trueTerrainColorsApplied = enabled;
        }
    }
}