using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

/// <summary>
/// Patch to disable WorldGrid:LongLatOf chache optimization.
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