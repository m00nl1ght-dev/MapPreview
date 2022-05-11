using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    
    public static readonly ThreadLocal<bool> debug = new(() => false);
    private static int idx;
    
    private static void SetData(uint seed, uint iterations) => data.Value = (ulong) seed | (ulong) iterations << 32;

    [HarmonyPrefix]
    [HarmonyPatch("Seed", MethodType.Setter)]
    private static bool Seed(int value)
    {
        SetData((uint) value, 0U);
        Debug("set seed " + value);
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
        Debug("getf with seed " + seed);
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
        Debug("geti with seed " + seed);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("PushState", new Type[0])]
    private static bool PushState()
    {
        stateStack.Value.Push(data.Value);
        Debug("push");
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("PopState")]
    private static bool PopState()
    {
        data.Value = stateStack.Value.Pop();
        Debug("pop");
        return false;
    }

    private static void Debug(string msg)
    {
        if (debug.Value)
        {
            var stackTrace = new StackTrace();
            var stackFrame = stackTrace.GetFrame(3);
            int i = 3; while (stackFrame?.GetMethod()?.DeclaringType == typeof(Rand)) stackFrame = stackTrace.GetFrame(i++);
            Log.Message(msg + " at " + stackFrame?.GetMethod()?.DeclaringType + "." + stackFrame?.GetMethod()?.Name + " idx " + (idx++));
        }
    }
}