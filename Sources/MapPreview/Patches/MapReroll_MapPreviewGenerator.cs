using System.Collections.Generic;
using HarmonyLib;
using MapReroll;
using UnityEngine;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(MapPreviewGenerator))]
public class MapReroll_MapPreviewGenerator
{
    [HarmonyPatch("GeneratePreviewForSeed")]
    private static void Prefix(string seed, int mapTile, int mapSize, Dictionary<string, Color> ___terrainColors)
    {
        Main.IsGeneratingPreview = true;
        TrueTerrainColors.UpdateTerrainColorsIfNeeded(___terrainColors);
        RimWorld_TerrainPatchMaker.Reset();
    }
    
    [HarmonyPatch("GeneratePreviewForSeed")]
    private static void Postfix(string seed, int mapTile, int mapSize)
    {
        RimWorld_TerrainPatchMaker.Reset();
        Main.IsGeneratingPreview = false;
    }
}