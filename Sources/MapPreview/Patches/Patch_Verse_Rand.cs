using System;
using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

/// <summary>
/// Holds a separate state for Verse.Rand on the preview thread.
/// All patches are conditional and only run while a preview is actually generating.
/// </summary>
[PatchGroup("Gen")]
[HarmonyPatch(typeof(Rand))]
internal static class Patch_Verse_Rand
{
    private static uint _seed = (uint) DateTime.Now.GetHashCode();
    private static uint _iterations;

    #if DEBUG
    #pragma warning disable CS0649

    [ThreadStatic]
    internal static bool DebugRand;

    [ThreadStatic]
    internal static int DebugCounter;

    #pragma warning restore CS0649
    #endif

    private static readonly Stack<ulong> _stateStack = new();

    private static ulong StateCompressed
    {
        get => _seed | (ulong) _iterations << 32;

        set
        {
            _seed = (uint) (value & uint.MaxValue);
            _iterations = (uint) (value >> 32 & uint.MaxValue);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("Seed", MethodType.Setter)]
    private static bool Seed(int value)
    {
        #if DEBUG
        if (DebugRand) MapPreviewAPI.Logger.Log($"Rand.Seed set to {value} [{DebugCounter++}]");
        #endif

        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        _seed = (uint) value;
        _iterations = 0U;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Value", MethodType.Getter)]
    private static bool Value(ref float __result)
    {
        #if DEBUG
        if (DebugRand) MapPreviewAPI.Logger.Log($"Rand.Value called [{DebugCounter++}]");
        #endif

        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __result = (float) ((MurmurHash.GetInt(_seed, _iterations++) - (double) int.MinValue) / uint.MaxValue);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Int", MethodType.Getter)]
    private static bool Int(ref int __result)
    {
        #if DEBUG
        if (DebugRand) MapPreviewAPI.Logger.Log($"Rand.Int called [{DebugCounter++}]");
        #endif

        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        __result = MurmurHash.GetInt(_seed, _iterations++);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("PushState", new Type[0])]
    private static bool PushState()
    {
        #if DEBUG
        if (DebugRand) MapPreviewAPI.Logger.Log($"Rand.PushState called [{DebugCounter++}]");
        #endif

        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        _stateStack.Push(StateCompressed);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("PushState", typeof(int))]
    private static bool PushState(int replacementSeed)
    {
        #if DEBUG
        if (DebugRand) MapPreviewAPI.Logger.Log($"Rand.PushState called with seed {replacementSeed} [{DebugCounter++}]");
        #endif

        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        _stateStack.Push(StateCompressed);
        _seed = (uint) replacementSeed;
        _iterations = 0U;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("PopState")]
    private static bool PopState()
    {
        #if DEBUG
        if (DebugRand) MapPreviewAPI.Logger.Log($"Rand.PopState called [{DebugCounter++}]");
        #endif

        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        StateCompressed = _stateStack.Pop();
        return false;
    }
}
