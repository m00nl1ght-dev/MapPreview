using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MapPreview.Patches;
using UnityEngine;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.ModCompat;

[StaticConstructorOnStartup]
internal static class ModCompat_MapReroll
{
    public static bool IsPresent { get; }
    
    static ModCompat_MapReroll()
    {
        try
        {
            var genType = GenTypes.GetTypeInAnyAssembly("MapReroll.MapPreviewGenerator");
            if (genType != null)
            {
                Log.Message(Main.LogPrefix + "Applying compatibility patches for Map Reroll.");
                Harmony harmony = new("Map Preview Map Reroll Compat");
                
                var genMethod = AccessTools.Method(genType, "GeneratePreviewForSeed");

                var self = typeof(ModCompat_MapReroll);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod genPrefix = new(self.GetMethod(nameof(GeneratePreviewForSeed_Prefix), bindingFlags));
                HarmonyMethod genPostfix = new(self.GetMethod(nameof(GeneratePreviewForSeed_Postfix), bindingFlags));

                harmony.Patch(genMethod, genPrefix, genPostfix);
                IsPresent = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(Main.LogPrefix + "Failed to apply compatibility patches for Map Reroll!");
            Debug.LogException(e);
        }
    }
    
    private static bool _trueTerrainColorsApplied;
    
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
    
    private static void GeneratePreviewForSeed_Prefix(Dictionary<string, Color> ___terrainColors)
    {
        Main.IsGeneratingPreview = true;
        UpdateTerrainColorsIfNeeded(___terrainColors);
        RimWorld_TerrainPatchMaker.Reset();
    }
    
    private static void GeneratePreviewForSeed_Postfix()
    {
        Main.IsGeneratingPreview = false;
        RimWorld_TerrainPatchMaker.Reset();
    }
}