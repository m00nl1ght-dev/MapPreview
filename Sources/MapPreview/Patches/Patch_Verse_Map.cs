using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Map))]
internal static class Patch_Verse_Map
{
    [HarmonyTranspiler]
    [HarmonyPatch("FillComponents")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> FillComponents_Filter_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("FillComponents_Filter")
            .MatchCall(typeof(Map), nameof(Map.GetComponent), [typeof(Type)])
            .ReplaceOperandWithMethod(typeof(Patch_Verse_Map), nameof(CheckSkipComponent))
            .Match(ci => ci.opcode == OpCodes.Brtrue_S || ci.opcode == OpCodes.Brtrue).Keep()
            .Greedy();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static bool CheckSkipComponent(Map map, Type type)
    {
        if (map.GetComponent(type) != null) return true;

        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return false;
        if (MapPreviewGenerator.IncludedMapComponentsFull.Contains(type.FullName)) return false;

        #if RW_1_6_OR_GREATER

        if (RandCompatCache.TryGetExpectedInterations(type, out var expectedIt))
        {
            Patch_Verse_Rand.SkipIterations(expectedIt);
        }

        #endif

        return true;
    }

    #if RW_1_6_OR_GREATER

    private static uint _prevRandIt;
    private static uint _prevRandCompIdx;

    [HarmonyPrefix]
    [HarmonyPatch("FillComponents")]
    [HarmonyPriority(-1)]
    private static void FillComponents_Prefix(Map __instance)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread) return;

        const uint expectedIterations = RandCompatCache.ExpectedRandIterationsInVanillaMapComponents;

        _prevRandIt = Rand.iterations;
        _prevRandCompIdx = 0;

        if (MapGenerator.mapBeingGenerated == __instance && _prevRandIt != expectedIterations)
        {
            MapPreviewAPI.Logger.Error(
                $"Map Preview has detected an issue causing previews to be inaccurate: " +
                $"Vanilla map components have modified the RNG state by {_prevRandIt} in their constructors, " +
                $"which does not match the expected amount of {expectedIterations} iterations." +
                $"Please report this on the Map Preview workshop page, so this issue can be fixed."
            );
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch("FillComponents")]
    [HarmonyPriority(Priority.Low)]
    private static IEnumerable<CodeInstruction> FillComponents_CheckRand_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pattern = TranspilerPattern.Build("FillComponents_CheckRand")
            .Match(ci => ci.opcode == OpCodes.Call && ci.operand is MethodInfo { Name: "CreateInstance" }).Keep()
            .InsertCall(typeof(Patch_Verse_Map), nameof(FillComponents_CheckRand))
            .Greedy();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static object FillComponents_CheckRand(object component)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread)
        {
            return component;
        }

        var randItAfter = Rand.iterations;

        if (component != null && _prevRandIt != randItAfter)
        {
            var actualIt = randItAfter - _prevRandIt;

            var type = component.GetType();
            var typeName = type.FullName ?? type.Name;

            var mcp = LoadedModManager.RunningMods.FirstOrDefault(m => m.assemblies.loadedAssemblies.Contains(type.Assembly));

            if (!RandCompatCache.TryGetExpectedInterations(type, out var expectedIt) || expectedIt != actualIt)
            {
                RandCompatCache.UpdateExpectedIterations(type, actualIt);
                RandCompatCache.Save();

                MapPreviewAPI.Logger.Warn(
                    $"Detected RNG usage in constructor of map component {typeName} ({_prevRandCompIdx}) from mod {mcp} with {actualIt}/{expectedIt} iterations. " +
                    $"Map Preview will take this into account from now on, but any previews generated until now may have been inaccurate."
                );
            }
            else
            {
                MapPreviewAPI.Logger.Debug($"Map component {typeName} from mod {mcp} used {expectedIt} RNG iterations as expected.");
            }
        }

        _prevRandIt = randItAfter;
        _prevRandCompIdx++;

        return component;
    }

    #endif

    #if DEBUG

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Map.FinalizeInit))]
    private static void FinalizeInit_Postfix()
    {
        var cameraDriverConfig = Find.CameraDriver.config;
        cameraDriverConfig.sizeRange.max = 200f;
        cameraDriverConfig.zoomSpeed = 5f;

        Find.PlaySettings.showWorldFeatures = false;
    }

    #endif
}
