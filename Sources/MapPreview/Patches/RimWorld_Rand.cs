using System;
using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

/// <summary>
/// Makes Verse.Rand thread-safe.
/// </summary>
[HarmonyPatch(typeof(Rand))]
internal static class RimWorld_Rand
{
    private static uint InitialSeed => (uint) DateTime.Now.GetHashCode();
    
    private static readonly ThreadLocal<ulong> data = new(() => InitialSeed);
    private static readonly ThreadLocal<Stack<ulong>> stateStack = new(() => new Stack<ulong>());
    
    private static void SetData(uint seed, uint iterations) => data.Value = seed | (ulong) iterations << 32;

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
        uint seed = (uint) (state & uint.MaxValue);
        uint iterations = (uint) (state >> 32 & uint.MaxValue);
        __result = (float) ((MurmurHash.GetInt(seed, iterations++) - (double) int.MinValue) / uint.MaxValue);
        SetData(seed, iterations);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Int", MethodType.Getter)]
    private static bool Int(ref int __result)
    {
        ulong state = data.Value;
        uint seed = (uint) (state & uint.MaxValue);
        uint iterations = (uint) (state >> 32 & uint.MaxValue);
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