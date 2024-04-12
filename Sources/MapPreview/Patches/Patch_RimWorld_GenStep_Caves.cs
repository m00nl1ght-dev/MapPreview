using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace MapPreview.Patches;

/// <summary>
/// Optimizes the performance of the vanilla cave generator, making it around 3 times as fast while producing the same result.
/// </summary>
[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Caves))]
internal static class Patch_RimWorld_GenStep_Caves
{
    [HarmonyTranspiler]
    [HarmonyPatch("Dig")]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> Dig_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => RemoveRedundantRehash(instructions, generator, "tmpGroupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("GetDistToCave")]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetDistToCave_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => RemoveRedundantRehash(instructions, generator, "tmpGroupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("FindRandomEdgeCellForTunnel")]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> FindRandomEdgeCellForTunnel_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => RemoveRedundantRehash(instructions, generator, "tmpGroupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("GetDistToNonRock", typeof(IntVec3), typeof(List<IntVec3>), typeof(IntVec3), typeof(int))]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetDistToNonRock_ByOffset_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => RemoveRedundantRehash(instructions, generator, "groupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("GetDistToNonRock", typeof(IntVec3), typeof(List<IntVec3>), typeof(float), typeof(int))]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetDistToNonRock_ByDir_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => RemoveRedundantRehash(instructions, generator, "groupSet");

    [HarmonyPostfix]
    [HarmonyPatch("RemoveSmallDisconnectedSubGroups")]
    private static void RemoveSmallDisconnectedSubGroups_Postfix(out HashSet<IntVec3> ___tmpGroupSet, HashSet<IntVec3> ___groupSet)
    {
        ___tmpGroupSet = ___groupSet;
    }

    private static IEnumerable<CodeInstruction> RemoveRedundantRehash(IEnumerable<CodeInstruction> instructions, ILGenerator generator, string fieldName)
    {
        var labelAfter = generator.DefineLabel();
        var ldargGroup = new CodeInstruction(OpCodes.Ldarg);
        var ldfldStatic = CodeInstruction.LoadField(typeof(GenStep_Caves), fieldName);

        var pattern = TranspilerPattern.Build("RemoveRedundantRehash")
            .MatchLoad(typeof(GenStep_Caves), fieldName).Nop() // to preserve labels
            .Insert(ldfldStatic)
            .Insert(CodeInstruction.Call(typeof(HashSet<IntVec3>), "get_Count"))
            .Insert(ldargGroup)
            .Insert(CodeInstruction.Call(typeof(List<IntVec3>), "get_Count"))
            .Insert(OpCodes.Ceq)
            .Insert(new CodeInstruction(OpCodes.Brtrue_S, labelAfter))
            .Insert(CodeInstruction.LoadField(typeof(GenStep_Caves), fieldName))
            .MatchCall(typeof(HashSet<IntVec3>), "Clear").Keep()
            .MatchLoad(typeof(GenStep_Caves), fieldName).Keep()
            .MatchLdarg().StoreIn(ldargGroup).Keep()
            .Match(ci => ci.operand is MethodInfo { Name: "AddRange" }).Keep()
            .MatchAny().Do(ci => ci.labels.Add(labelAfter))
            .Greedy();

        return TranspilerPattern.Apply(instructions, pattern);
    }
}
