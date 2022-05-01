using System;
using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using Verse;

// ReSharper disable All

namespace MapPreview.Patches;

/// <summary>
/// Makes Verse.Rand thread-safe.
/// </summary>
[HarmonyPatch(typeof(Rand))]
internal static class RimWorld_Rand
{
    private static uint InitialSeed => (uint) DateTime.Now.GetHashCode();
    
    private static readonly ThreadLocal<ulong> data = new(() => (ulong) InitialSeed);
    private static readonly ThreadLocal<Stack<ulong>> stateStack = new(() => new Stack<ulong>());
    
    private static void SetData(uint seed, uint iterations) => data.Value = (ulong) seed | (ulong) iterations << 32;

    [HarmonyPrefix]
    [HarmonyPatch("Seed", MethodType.Setter)]
    private static bool Seed(int value)
    {
        SetData((uint) value, 0U);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Value", MethodType.Getter)]
    private static bool Value(ref float __result)
    {
        ulong state = data.Value;
        uint seed = (uint) (state & (ulong) uint.MaxValue);
        uint iterations = (uint) (state >> 32 & (ulong) uint.MaxValue);
        __result = (float) (((double) MurmurHash.GetInt(seed, iterations++) - (double) int.MinValue) / (double) uint.MaxValue);
        SetData(seed, iterations);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Int", MethodType.Getter)]
    private static bool Int(ref int __result)
    {
        ulong state = data.Value;
        uint seed = (uint) (state & (ulong) uint.MaxValue);
        uint iterations = (uint) (state >> 32 & (ulong) uint.MaxValue);
        __result = MurmurHash.GetInt(seed, iterations++);
        SetData(seed, iterations);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("PushState", new Type[0])]
    private static bool PushState()
    {
        stateStack.Value.Push(data.Value);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("PopState")]
    private static bool PopState()
    {
        data.Value = stateStack.Value.Pop();
        return false;
    }
}