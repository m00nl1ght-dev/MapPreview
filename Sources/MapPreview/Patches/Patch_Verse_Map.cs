using System;
using System.Collections.Generic;
using System.Linq;
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

        if (MapPreviewGenerator.ExpectedRandIterationsInMapComponents.TryGetValue(type.FullName ?? type.Name, out var expectedIt))
        {
            Patch_Verse_Rand.SkipIterations(expectedIt);
        }

        #endif

        return true;
    }

    #if RW_1_6_OR_GREATER

    [HarmonyPrefix]
    [HarmonyPatch("FillComponents")]
    private static void FillComponents_Prefix(Map __instance)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread) return;

        const uint expectedIterations = MapPreviewGenerator.ExpectedRandIterationsInVanillaMapComponents;

        if (MapGenerator.mapBeingGenerated == __instance && Rand.iterations != expectedIterations)
        {
            MapPreviewAPI.Logger.Error(
                $"Map Preview has detected an issue causing previews to be inaccurate: " +
                $"Vanilla map components have modified the RNG state by {Rand.iterations} in their constructors, " +
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
            .MatchCall(typeof(Activator), nameof(Activator.CreateInstance), [typeof(Type), typeof(object[])])
            .ReplaceOperandWithMethod(typeof(Patch_Verse_Map), nameof(CreateInstance_Wrapper_CheckRand))
            .Greedy();

        return TranspilerPattern.Apply(instructions, pattern);
    }

    private static object CreateInstance_Wrapper_CheckRand(Type type, object[] args)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread)
        {
            return Activator.CreateInstance(type, args);
        }

        var randItBefore = Rand.iterations;
        var instance = Activator.CreateInstance(type, args);
        var randItAfter = Rand.iterations;

        if (randItBefore != randItAfter)
        {
            var actualIt = randItAfter - randItBefore;
            var typeName = type.FullName ?? type.Name;

            var mcp = LoadedModManager.RunningMods.FirstOrDefault(m => m.assemblies.loadedAssemblies.Contains(type.Assembly));

            if (MapPreviewGenerator.ExpectedRandIterationsInMapComponents.TryGetValue(typeName, out var expectedIt))
            {
                if (expectedIt == actualIt)
                {
                    MapPreviewAPI.Logger.Debug($"Map component {typeName} from mod {mcp} used {expectedIt} RNG iterations as expected.");
                }
                else
                {
                    MapPreviewAPI.Logger.Error(
                        $"Map Preview has detected a compatibility issue causing previews to be inaccurate: " +
                        $"Map component {typeName} from mod {mcp} has modified the RNG state by {actualIt} in its constructor, " +
                        $"which does not match the expected amount of {expectedIt} iterations." +
                        $"Please report this on the Map Preview workshop page, so this compatibility issue can be fixed."
                    );
                }
            }
            else
            {
                MapPreviewAPI.Logger.Error(
                    $"Map Preview has detected a compatibility issue causing previews to be inaccurate: " +
                    $"Map component {typeName} from mod {mcp} has modified the RNG state by {actualIt} in its constructor. " +
                    $"Please report this on the Map Preview workshop page, so this compatibility issue can be fixed."
                );
            }
        }

        return instance;
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
