using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

/// <summary>
/// Patch to conditionally disable ComponentCache from the SmashTools utility mod used by The Vehicle Framework.
/// That feature otherwise causes preview generation to fail.
/// </summary>
[HarmonyPatch]
internal class ModCompat_SmashTools : ModCompat
{
    public override string TargetAssemblyName => "SmashTools";
    public override string DisplayName => "SmashTools from The Vehicle Framework";

    [HarmonyPrefix]
    [HarmonyPatch("SmashTools.ComponentCache", "MapGenerated")]
    private static bool ComponentCache_MapGenerated()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
