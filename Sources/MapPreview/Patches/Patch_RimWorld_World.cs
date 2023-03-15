using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(World))]
internal static class Patch_RimWorld_World
{
    internal static readonly Type Self = typeof(Patch_RimWorld_World);

    [ThreadStatic]
    private static List<int> tmpNeighbors = null;

    [ThreadStatic]
    private static List<Rot4> tmpOceanDirs = null;

    [ThreadStatic]
    private static List<ThingDef> tmpNaturalRockDefs = null;

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(World.CoastDirectionAt))]
    [HarmonyPriority(Priority.VeryLow)]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> CoastDirectionAt_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var begin = generator.DefineLabel();

        var constructorInt = AccessTools.Constructor(typeof(List<int>), Type.EmptyTypes) ?? throw new Exception();
        var constructorRot4 = AccessTools.Constructor(typeof(List<Rot4>), Type.EmptyTypes) ?? throw new Exception();

        var setup = new List<CodeInstruction>
        {
            CodeInstruction.LoadField(Self, nameof(tmpNeighbors)),
            new(OpCodes.Brtrue, begin),
            new(OpCodes.Newobj, constructorInt),
            CodeInstruction.StoreField(Self, nameof(tmpNeighbors)),
            new(OpCodes.Newobj, constructorRot4),
            CodeInstruction.StoreField(Self, nameof(tmpOceanDirs))
        };

        var tsNeighbors = TranspilerPattern.Build("ThreadStaticNeighbors")
            .MatchLoad(typeof(World), "tmpNeighbors")
            .ReplaceOperandWithField(Self, nameof(tmpNeighbors))
            .Greedy(0);

        var tsOceanDirs = TranspilerPattern.Build("ThreadStaticOceanDirs")
            .MatchLoad(typeof(World), "tmpOceanDirs")
            .ReplaceOperandWithField(Self, nameof(tmpOceanDirs))
            .Greedy(0);

        instructions.First().labels.Add(begin);
        return setup.Concat(TranspilerPattern.Apply(instructions, tsNeighbors, tsOceanDirs));
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.VeryLow)]
    [PatchExcludedFromConflictCheck]
    private static IEnumerable<CodeInstruction> NaturalRockTypesIn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var begin = generator.DefineLabel();

        var constructor = AccessTools.Constructor(typeof(List<ThingDef>), Type.EmptyTypes) ?? throw new Exception();

        var setup = new List<CodeInstruction>
        {
            CodeInstruction.LoadField(Self, nameof(tmpNaturalRockDefs)),
            new(OpCodes.Brtrue, begin),
            new(OpCodes.Newobj, constructor),
            CodeInstruction.StoreField(Self, nameof(tmpNaturalRockDefs))
        };

        var pattern = TranspilerPattern.Build("ThreadStaticNaturalRockDefs")
            .MatchLoad(typeof(World), "tmpNaturalRockDefs")
            .ReplaceOperandWithField(Self, nameof(tmpNaturalRockDefs))
            .Greedy(0);

        instructions.First().labels.Add(begin);
        return setup.Concat(TranspilerPattern.Apply(instructions, pattern));
    }
}
