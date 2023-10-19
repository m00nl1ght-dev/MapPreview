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
                .Insert(CodeInstruction.LoadField(typeof(MapPreviewGenerator), nameof(MapPreviewGenerator.IncludedMapComponentsFull)))
                .Insert(ldlocType)
                .Insert(CodeInstruction.Call(typeof(Type), "get_FullName"))
                .Insert(CodeInstruction.Call(typeof(HashSet<string>), "Contains", new[] { typeof(string) }))
                .Insert(brfalseSkip);

            return TranspilerPattern.Apply(instructions, pattern);
        }

        _ = Transpiler(null);
    }
}
