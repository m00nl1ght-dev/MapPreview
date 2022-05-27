using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MapPreview.Patches;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

/// <summary>
/// Patch to disable WorldGrid:LongLatOf chache optimization.
/// That feature otherwise causes the game to freeze and all sorts of other problems during preview generation.
/// </summary>
public static class ModCompat_PerformanceOptimizer
{
    public static bool IsPresent { get; private set; }
    
    public static void Apply()
    {
        try
        {
            var opType = GenTypes.GetTypeInAnyAssembly("PerformanceOptimizer.Optimization_WorldGrid_LongLatOf");
            if (opType != null)
            {
                Log.Message(ModInstance.LogPrefix + "Applying compatibility patches for Performance Optimizer.");
                Harmony harmony = new("Map Preview Performance Optimizer Compat");

                var doPatches = AccessTools.Method(opType, "DoPatches");

                var self = typeof(ModCompat_PerformanceOptimizer);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod doPatchesPrefix = new(self.GetMethod(nameof(Optimization_WorldGrid_LongLatOf_Prefix), bindingFlags));

                harmony.Patch(doPatches, doPatchesPrefix);
                IsPresent = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(ModInstance.LogPrefix + "Failed to apply compatibility patches for Performance Optimizer!");
            Debug.LogException(e);
        }
    }
    
    private static bool Optimization_WorldGrid_LongLatOf_Prefix()
    {
        return false;
    }
}