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
            .Match(OpCodes.Brtrue_S).Keep()
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

    /// <summary>
    /// Vanilla GasGrid.ctor calls Rand once. ConstructMinimalMapComponents leaves that object commented out (trial) and skips one iteration in its place.
    /// Map.ConstructComponents can call Rand more times before FillComponents. The one-iteration skip then leaves the preview RNG behind the map, and comparing the live count to 1 logged that on every map.
    /// Until a measured pre-fill count is that one call, the preview runs ConstructComponents so the same calls advance the preview RNG.
    /// A later count of one does not switch back.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch("FillComponents")]
    [HarmonyPriority(-1)]
    private static void FillComponents_Prefix(Map __instance)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread)
        {
            RandCompatCache.NotePreFillIterations(Patch_Verse_Rand.PreviewIterations);
            return;
        }

        _prevRandIt = Rand.iterations;
        _prevRandCompIdx = 0;

        if (MapGenerator.mapBeingGenerated == __instance)
            RandCompatCache.NotePreFillIterations(_prevRandIt);
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
