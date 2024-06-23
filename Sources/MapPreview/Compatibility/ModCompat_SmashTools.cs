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
    public override string DisplayName => "SmashTools from Vehicle Framework";

    protected override bool OnApply()
    {
        // The target method has been removed in VF 1.5.1659
        // This patch is conditional now and only applies to old versions of VF
        return AccessTools.Method("SmashTools.ComponentCache:MapGenerated") != null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SmashTools.ComponentCache", "MapGenerated")]
    private static bool ComponentCache_MapGenerated()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
