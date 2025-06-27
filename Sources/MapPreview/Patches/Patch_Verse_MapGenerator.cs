using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
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

    [HarmonyTranspiler]
    [HarmonyPatch("GenerateMap")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> GenerateMap_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("GenerateMap")
            .MatchCall(typeof(Gen), nameof(Gen.HashCombineInt), [typeof(int), typeof(int)]).Keep()
            .Insert(OpCodes.Ldarg_1)
            .Insert(CodeInstruction.Call(typeof(Patch_Verse_MapGenerator), nameof(AdjustSeedIfNeeded)))
            .MatchStloc().Keep();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static int AdjustSeedIfNeeded(int seed, MapParent mapParent)
    {
        if (mapParent != null && SeedRerollData.IsMapSeedRerolled(Find.World, mapParent.Tile, out var savedSeed))
            return savedSeed;

        return seed;
    }
}
