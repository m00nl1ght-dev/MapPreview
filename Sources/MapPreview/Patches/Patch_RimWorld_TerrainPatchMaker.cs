using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace MapPreview.Patches;

/// <summary>
/// Ensures that TerrainPatchMakers get a deterministic seed, if needed.
/// Has priority over the buggy patches from Map Reroll that cause all TerrainPatchMakers to have the same seed.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(TerrainPatchMaker))]
internal static class Patch_RimWorld_TerrainPatchMaker
{
    internal static readonly Type Self = typeof(Patch_RimWorld_TerrainPatchMaker);

    [HarmonyPrefix]
    [HarmonyPatch("Init")]
    [HarmonyPriority(750)]
    private static bool Init_Prefix(TerrainPatchMaker __instance, Map map)
    {
        if (map.Tile < 0 || !MapPreviewAPI.ShouldUseStableSeed.Apply(map)) return true;
        Init_WithStableSeed(__instance, map);
        return false;
    }

    [HarmonyPatch("Init")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow)]
    private static void Init_WithStableSeed(TerrainPatchMaker __instance, Map map)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var pattern = TranspilerPattern.Build("UseStableSeed")
                .Match(OpCodes.Ldc_I4_0).Replace(OpCodes.Ldarg_0)
                .Match(OpCodes.Ldc_I4).Replace(OpCodes.Ldarg_1)
                .MatchCall(typeof(Rand), "Range", new[] { typeof(int), typeof(int) }).Remove()
                .Insert(CodeInstruction.Call(Self, nameof(MakeStableSeed)));

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null);
    }

    private static int MakeStableSeed(TerrainPatchMaker tpm, Map map)
    {
        int idx = map.Biome.terrainPatchMakers.IndexOf(tpm);
        var mapSeed = SeedRerollData.GetMapSeed(Find.World, map.Tile);

        if (idx >= 0) return mapSeed ^ 9305 + idx;
        return mapSeed ^ GetHashCode(tpm);
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
