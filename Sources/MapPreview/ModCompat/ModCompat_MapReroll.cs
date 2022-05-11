using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

public static class ModCompat_MapReroll
{
    public static bool IsPresent { get; private set; }
    
    public static void Apply()
    {
        try
        {
            var genType = GenTypes.GetTypeInAnyAssembly("MapReroll.MapPreviewGenerator");
            if (genType != null)
            {
                Log.Message("[Map Preview] Applying compatibility patches for Map Reroll.");
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
        catch
        {
            Log.Error("[Map Preview] Failed to apply compatibility patches for Map Reroll!");
        }
    }
    
    private static void GeneratePreviewForSeed_Prefix(Dictionary<string, Color> ___terrainColors)
    {
        Main.IsGeneratingPreview = true;
        TrueTerrainColors.UpdateTerrainColorsIfNeeded(___terrainColors);
        RimWorld_TerrainPatchMaker.Reset();
    }
    
    private static void GeneratePreviewForSeed_Postfix()
    {
        Main.IsGeneratingPreview = false;
        RimWorld_TerrainPatchMaker.Reset();
    }
}