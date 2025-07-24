#if !RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

/// <summary>
/// Patch to disable WorldGrid:LongLatOf cache optimization.
/// That feature otherwise causes the game to freeze and all sorts of other problems during preview generation.
/// </summary>
[HarmonyPatch]
internal class ModCompat_PerformanceOptimizer : ModCompat
{
    public override string TargetAssemblyName => "PerformanceOptimizer";
    public override string DisplayName => "Performance Optimizer";

    [HarmonyPrefix]
    [HarmonyPatch("PerformanceOptimizer.Optimization_WorldGrid_LongLatOf", "Prefix")]
    private static bool Optimization_WorldGrid_LongLatOf_Prefix()
    {
        return !MapPreviewAPI.IsGeneratingPreview;
    }
}

#endif
