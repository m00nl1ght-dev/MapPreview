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
    [HarmonyPriority(Priority.First)]
    private static void GenerateMap()
    {
        if (MapPreviewAPI.IsGeneratingPreview)
        {
            MapPreviewAPI.Logger.Warn("Something attempted to use the MapGenerator while a preview is being generated, waiting for it to complete!");

            if (!MapPreviewGenerator.Instance.WaitUntilIdle(60))
            {
                throw new Exception("Timeout reached while waiting for a map preview to finish generating!");
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("GenerateContentsIntoMap")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static void GenerateContentsIntoMap(Map map, ref int seed)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread) return;
        if (map.Tile >= 0 && SeedRerollData.IsMapSeedRerolled(Find.World, map.Tile, out var savedSeed))
        {
            seed = savedSeed;
        }
    }
}
