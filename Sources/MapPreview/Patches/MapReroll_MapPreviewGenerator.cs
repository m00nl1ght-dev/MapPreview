using HarmonyLib;
using MapReroll;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(MapPreviewGenerator))]
public class MapReroll_MapPreviewGenerator
{
    [HarmonyPatch("GeneratePreviewForSeed")]
    private static void Prefix(string seed, int mapTile, int mapSize)
    {
        RimWorld_TerrainPatchMaker.Reset();
    }
    
    [HarmonyPatch("GeneratePreviewForSeed")]
    private static void Postfix(string seed, int mapTile, int mapSize)
    {
        RimWorld_TerrainPatchMaker.Reset();
    }
}