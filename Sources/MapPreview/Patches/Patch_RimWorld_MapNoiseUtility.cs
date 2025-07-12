#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace MapPreview.Patches;

/// <summary>
/// Some tile mutators directly use the tileId as noise seed, so rerolling has no effect on them.
/// This patch adds an offset to the noise seed equivalent to the one on the rerolled map seed.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(MapNoiseUtility))]
internal static class Patch_RimWorld_MapNoiseUtility
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MapNoiseUtility.AddDisplacementNoise))]
    private static void AddDisplacementNoise_Prefix(ref int seed)
    {
        var world = Find.World;
        var map = MapGenerator.mapBeingGenerated;

        if (map != null && seed == map.Tile.tileId)
        {
            if (SeedRerollData.IsMapSeedRerolled(world, map.Tile, out var mapSeed))
            {
                var originalMapSeed = SeedRerollData.GetOriginalMapSeed(world, map.Tile);
                seed += mapSeed - originalMapSeed;
            }
        }
    }
}

#endif
