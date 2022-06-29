using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

/// <summary>
/// Holds a separate state for Verse.Rand on the preview thread.
/// All patches are conditional and only run while a preview is actually generating.
/// </summary>
[HarmonyPatch(typeof(Rand))]
internal static class RimWorld_Rand
{
    private static uint seed = (uint) DateTime.Now.GetHashCode();
    private static uint iterations;
    
    private static readonly Stack<ulong> stateStack = new();
    
    private static ulong StateCompressed
    {
        get => seed | (ulong) iterations << 32;
        set
        {
            seed = (uint) (value & uint.MaxValue);
            iterations = (uint) (value >> 32 & uint.MaxValue);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("Seed", MethodType.Setter)]
    private static bool Seed(int value)
    {
        if (!Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        seed = (uint) value;
        iterations = 0U;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Value", MethodType.Getter)]
    private static bool Value(ref float __result)
    {
        if (!Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __result = (float) ((MurmurHash.GetInt(seed, iterations++) - (double) int.MinValue) / uint.MaxValue);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Int", MethodType.Getter)]
    private static bool Int(ref int __result)
    {
        if (!Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __result = MurmurHash.GetInt(seed, iterations++);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("PushState", new Type[0])]
    private static bool PushState()
    {
        if (!Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        stateStack.Push(StateCompressed);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("PushState", typeof(int))]
    private static bool PushState(int replacementSeed)
    {
        if (!Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        stateStack.Push(StateCompressed);
        seed = (uint) replacementSeed;
        iterations = 0U;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("PopState")]
    private static bool PopState()
    {
        if (!Main.IsGeneratingPreview || !ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        StateCompressed = stateStack.Pop();
        return false;
    }
}