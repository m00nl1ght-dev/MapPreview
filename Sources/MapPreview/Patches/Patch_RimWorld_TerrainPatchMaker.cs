using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;
using Verse.Noise;

namespace MapPreview.Patches;

/// <summary>
/// Ensures that TerrainPatchMakers get a deterministic seed, if needed.
/// Has priority over the buggy patches from Map Reroll that cause all TerrainPatchMakers to have the same seed.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(TerrainPatchMaker))]
internal static class Patch_RimWorld_TerrainPatchMaker
{
    [HarmonyPrefix]
    [HarmonyPatch("Init")]
    [HarmonyPriority(750)]
    private static bool Init(Map map, ref ModuleBase ___noise, ref Map ___currentlyInitializedForMap, TerrainPatchMaker __instance)
    {
        if (!MapPreviewAPI.ShouldUseStableSeed(map)) return true;

        ___noise = new Perlin(
            __instance.perlinFrequency,
            __instance.perlinLacunarity,
            __instance.perlinPersistence,
            __instance.perlinOctaves,
            MakeStableSeed(__instance, map),
            QualityMode.Medium);

        ___currentlyInitializedForMap = map;
        return false;
    }

    private static int MakeStableSeed(TerrainPatchMaker tpm, Map map)
    {
        int idx = map.Biome.terrainPatchMakers.IndexOf(tpm);
        var seed = SeedRerollData.IsMapSeedRerolled(Find.World, map.Tile, out var savedSeed)
            ? savedSeed : Find.World.info.Seed ^ map.Tile;

        if (idx >= 0) return seed ^ 9305 + idx;
        return seed ^ GetHashCode(tpm);
    }

    public static int GetHashCode(TerrainPatchMaker tpm)
    {
        unchecked
        {
            var hashCode = tpm.perlinFrequency.GetHashCode();
            hashCode = (hashCode * 397) ^ tpm.perlinLacunarity.GetHashCode();
            hashCode = (hashCode * 397) ^ tpm.perlinPersistence.GetHashCode();
            hashCode = (hashCode * 397) ^ tpm.perlinOctaves;
            hashCode = (hashCode * 397) ^ tpm.minFertility.GetHashCode();
            hashCode = (hashCode * 397) ^ tpm.maxFertility.GetHashCode();
            hashCode = (hashCode * 397) ^ tpm.minSize;

            foreach (var threshold in tpm.thresholds)
            {
                hashCode = (hashCode * 397) ^ threshold.min.GetHashCode();
                hashCode = (hashCode * 397) ^ threshold.max.GetHashCode();
                hashCode = (hashCode * 397) ^ threshold.terrain.defName.GetHashCode();
            }

            return hashCode;
        }
    }
}
