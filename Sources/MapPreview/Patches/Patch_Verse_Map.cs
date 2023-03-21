using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Map))]
internal static class Patch_Verse_Map
{
    private static readonly HashSet<string> IncludedMapComponents = new()
    {
        // Vanilla
        typeof(RoadInfo).FullName,
        typeof(WaterInfo).FullName,

        // Geological Landforms
        "GeologicalLandforms.BiomeGrid",

        // Water Freezes
        "ActiveTerrain.SpecialTerrainList",

        // VFE Core
        "VFECore.SpecialTerrainList",

        // Alpha Biomes
        "AlphaBiomes.MapComponentExtender",

        // Dubs Bad Hygiene
        "DubsBadHygiene.MapComponent_Hygiene",

        // Dubs Paint Shop
        "DubRoss.MapComponent_PaintShop"
    };

    internal static readonly Type Self = typeof(Patch_Verse_Map);

    [HarmonyPrefix]
    [HarmonyPatch("FillComponents")]
    private static bool FillComponents_Prefix(Map __instance)
    {
        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        FillComponents_ForPreviewMap(__instance);
        return false;
    }

    [HarmonyPatch("FillComponents")]
    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.VeryLow)]
    private static void FillComponents_ForPreviewMap(Map __instance)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var ldlocType = new CodeInstruction(OpCodes.Ldloc);
            var brfalseSkip = new CodeInstruction(OpCodes.Brfalse_S);

            var pattern = TranspilerPattern.Build("FillComponents")
                .MatchLdloc().StoreOperandIn(ldlocType).Keep()
                .MatchCall(typeof(Map), nameof(Map.GetComponent), new[] { typeof(Type) }).Keep()
                .Match(OpCodes.Brtrue_S).StoreOperandIn(brfalseSkip).Keep()
                .Insert(CodeInstruction.LoadField(Self, nameof(IncludedMapComponents)))
                .Insert(ldlocType)
                .Insert(CodeInstruction.Call(typeof(Type), "get_FullName"))
                .Insert(CodeInstruction.Call(typeof(HashSet<string>), "Contains", new[] { typeof(string) }))
                .Insert(brfalseSkip);

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null);
    }
}
