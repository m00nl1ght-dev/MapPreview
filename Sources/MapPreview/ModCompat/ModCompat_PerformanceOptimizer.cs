using System.Reflection;
using HarmonyLib;
using Verse;

namespace MapPreview.Patches;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

/// <summary>
/// Patch to disable WorldGrid:LongLatOf chache optimization  during preview generation.
/// That feature otherwise causes the game to freeze.
/// </summary>
public static class ModCompat_PerformanceOptimizer
{
    public static void Apply()
    {
        try
        {
            var opType = GenTypes.GetTypeInAnyAssembly("PerformanceOptimizer.Optimization_WorldGrid_LongLatOf");
            if (opType != null)
            {
                Log.Message("[Map Preview] Applying compatibility patches for Performance Optimizer.");
                Harmony harmony = new("Map Preview Performance Optimizer Compat");

                var opPrefix = AccessTools.Method(opType, "Prefix");

                var self = typeof(ModCompat_PerformanceOptimizer);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod methodPatchPrefix = new(self.GetMethod(nameof(Optimization_WorldGrid_LongLatOf_Prefix), bindingFlags));

                harmony.Patch(opPrefix, methodPatchPrefix);
            }
        }
        catch
        {
            Log.Error("[Map Preview] Failed to apply compatibility patches for Performance Optimizer!");
        }
    }
    
    private static bool Optimization_WorldGrid_LongLatOf_Prefix()
    {
        return !Main.IsGeneratingPreview;
    }
}