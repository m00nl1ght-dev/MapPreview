using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_VE_VEF : ModCompat
{
    #if RW_1_6_OR_GREATER
    public override string TargetAssemblyName => "VEF";
    #else
    public override string TargetAssemblyName => "VFECore";
    #endif

    public override string DisplayName => "Vanilla Expanded Framework";

    [HarmonyPrefix]
    #if RW_1_6_OR_GREATER
    [HarmonyPatch("VEF.Maps.VanillaExpandedFramework_TerrainGrid_SetTerrain", "Postfix")]
    #else
    [HarmonyPatch("VFECore._TerrainGrid", "Postfix")]
    #endif
    private static bool TerrainGrid_SetTerrain_Postfix()
    {
        return !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
