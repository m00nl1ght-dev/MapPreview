using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace MapPreview.Patches;

[PatchGroup("Active")]
[HarmonyPatch(typeof(WorldInterface))]
internal class Patch_RimWorld_WorldInterface
{
    [HarmonyTranspiler]
    [HarmonyPatch("WorldInterfaceUpdate")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> WorldInterfaceUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("WorldInterfaceUpdate")
            .Insert(OpCodes.Ldarg_0)
            .Insert(CodeInstruction.LoadField(typeof(WorldInterface), nameof(WorldInterface.selector)))
            .Insert(CodeInstruction.Call(typeof(WorldInterfaceManager), nameof(WorldInterfaceManager.UpdateWhileWorldShown)))
            .Match(OpCodes.Br_S).Keep()
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchLoad(typeof(WorldInterface), nameof(WorldInterface.targeter)).Keep()
            .MatchCall(typeof(WorldTargeter), nameof(WorldTargeter.StopTargeting)).Keep()
            .Match(OpCodes.Ldarg_0).Keep()
            .MatchLoad(typeof(WorldInterface), nameof(WorldInterface.tilePicker)).Keep()
            .MatchCall(typeof(TilePicker), nameof(TilePicker.StopTargeting)).Keep()
            .Insert(CodeInstruction.Call(typeof(WorldInterfaceManager), nameof(WorldInterfaceManager.UpdateWhileWorldHidden)));

        return TranspilerPattern.Apply(instructions, pattern);
    }
}
