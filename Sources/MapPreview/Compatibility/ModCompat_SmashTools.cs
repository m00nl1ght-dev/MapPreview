using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

/// <summary>
/// Patch to conditionally disable ComponentCache from the SmashTools utility mod used by The Vehicle Framework.
/// That feature otherwise causes preview generation to fail.
/// </summary>
[HarmonyPatch]
internal class ModCompat_SmashTools : ModCompat
{
    public override string TargetAssembly => "SmashTools";
    public override string DisplayName => "SmashTools from The Vehicle Framework";

    [HarmonyPrefix]
    [HarmonyPatch("SmashTools.ComponentCache", "MapGenerated")]
    private static bool ComponentCache_MapGenerated()
    {
        return !Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}