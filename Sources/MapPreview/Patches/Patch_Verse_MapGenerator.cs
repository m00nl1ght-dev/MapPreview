using System;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MapGenerator))]
internal static class Patch_Verse_MapGenerator
{
    [HarmonyPrefix]
    [HarmonyPatch("GenerateMap")]
    private static bool GenerateMap()
    {
        if (!MapPreviewAPI.IsGeneratingPreview) return true;
        throw new Exception("Attempted to use MapGenerator while a map preview is being generated!");
    }

    [HarmonyPrefix]
    [HarmonyPatch("GenerateContentsIntoMap")]
    private static void GenerateContentsIntoMap(Map map, ref int seed)
    {
        if (MapPreviewAPI.IsGeneratingPreview) return;
        if (SeedRerollData.IsMapSeedRerolled(Find.World, map.Tile, out var savedSeed))
        {
            seed = savedSeed;
        }
    }
}
