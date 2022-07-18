using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Noise;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

/// <summary>
/// Ensures that TerrainPatchMakers get a deterministic seed.
/// Has priority over the buggy patches from Map Reroll that cause all TerrainPatchMakers to have the same seed.
/// </summary>
[HarmonyPatch(typeof(TerrainPatchMaker), "Init")]
internal static class RimWorld_TerrainPatchMaker
{
    private static int _instanceIdx;

    [HarmonyPriority(750)]
    private static bool Prefix(Map map, ref ModuleBase ___noise, ref Map ___currentlyInitializedForMap, TerrainPatchMaker __instance)
    {
        int seed = Main.TpmSeedSource?.Invoke(__instance, map.Tile) ?? Find.World.info.Seed ^ map.Tile ^ 9305 + _instanceIdx;
        ___noise = new Perlin(__instance.perlinFrequency, __instance.perlinLacunarity, __instance.perlinPersistence, __instance.perlinOctaves, seed, QualityMode.Medium);
        NoiseDebugUI.RenderSize = new IntVec2(map.Size.x, map.Size.z);
        NoiseDebugUI.StoreNoiseRender(___noise, "TerrainPatchMaker " + _instanceIdx);
        ___currentlyInitializedForMap = map;
        _instanceIdx++;
        return false;
    }

    public static void Reset()
    {
        _instanceIdx = 0;
    }
}