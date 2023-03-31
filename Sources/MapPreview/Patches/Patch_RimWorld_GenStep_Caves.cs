using System.Collections.Generic;
using System.Reflection;
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
    private static IEnumerable<CodeInstruction> Dig_Transpiler(IEnumerable<CodeInstruction> instructions)
        => RemoveRedundantRehash(instructions, "tmpGroupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("GetDistToCave")]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetDistToCave_Transpiler(IEnumerable<CodeInstruction> instructions)
        => RemoveRedundantRehash(instructions, "tmpGroupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("FindRandomEdgeCellForTunnel")]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> FindRandomEdgeCellForTunnel_Transpiler(IEnumerable<CodeInstruction> instructions)
        => RemoveRedundantRehash(instructions, "tmpGroupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("GetDistToNonRock", typeof(IntVec3), typeof(List<IntVec3>), typeof(IntVec3), typeof(int))]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetDistToNonRock_ByOffset_Transpiler(IEnumerable<CodeInstruction> instructions)
        => RemoveRedundantRehash(instructions, "groupSet");

    [HarmonyTranspiler]
    [HarmonyPatch("GetDistToNonRock", typeof(IntVec3), typeof(List<IntVec3>), typeof(float), typeof(int))]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetDistToNonRock_ByDir_Transpiler(IEnumerable<CodeInstruction> instructions)
        => RemoveRedundantRehash(instructions, "groupSet");

    [HarmonyPostfix]
    [HarmonyPatch("RemoveSmallDisconnectedSubGroups")]
    private static void RemoveSmallDisconnectedSubGroups_Postfix(out HashSet<IntVec3> ___tmpGroupSet, HashSet<IntVec3> ___groupSet)
    {
        ___tmpGroupSet = ___groupSet;
    }

    private static IEnumerable<CodeInstruction> RemoveRedundantRehash(IEnumerable<CodeInstruction> instructions, string fieldName)
    {
        var pattern = TranspilerPattern.Build("RemoveRedundantRehash")
            .MatchLoad(typeof(GenStep_Caves), fieldName).Nop()
            .MatchCall(typeof(HashSet<IntVec3>), "Clear").Nop()
            .MatchLoad(typeof(GenStep_Caves), fieldName).Nop()
            .MatchLdarg().Nop()
            .Match(ci => ci.operand is MethodInfo { Name: "AddRange" }).Nop()
            .Greedy();

        return TranspilerPattern.Apply(instructions, pattern);
    }
}
