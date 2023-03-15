using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldGrid))]
internal static class Patch_RimWorld_WorldGrid
{
    internal static readonly Type Self = typeof(Patch_RimWorld_WorldGrid);

    [ThreadStatic]
    private static List<int> tmpNeighbors = null;

    private static IEnumerable<CodeInstruction> InjectThreadStatic(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var begin = generator.DefineLabel();

        var constructor = AccessTools.Constructor(typeof(List<int>), Type.EmptyTypes) ?? throw new Exception();

        var setup = new List<CodeInstruction>
        {
            CodeInstruction.LoadField(Self, nameof(tmpNeighbors)),
            new(OpCodes.Brtrue, begin),
            new(OpCodes.Newobj, constructor),
            CodeInstruction.StoreField(Self, nameof(tmpNeighbors))
        };

        var pattern = TranspilerPattern.Build("ThreadStaticNeighbors")
            .MatchLoad(typeof(WorldGrid), "tmpNeighbors")
            .ReplaceOperandWithField(Self, nameof(tmpNeighbors))
            .Greedy(0);

        instructions.First().labels.Add(begin);
        return setup.Concat(TranspilerPattern.Apply(instructions, pattern));
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(WorldGrid.IsNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> IsNeighbor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => InjectThreadStatic(instructions, generator);

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(WorldGrid.GetNeighborId))]
    [HarmonyPriority(Priority.VeryLow)]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetNeighborId_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => InjectThreadStatic(instructions, generator);

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(WorldGrid.GetTileNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> GetTileNeighbor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        => InjectThreadStatic(instructions, generator);
}
