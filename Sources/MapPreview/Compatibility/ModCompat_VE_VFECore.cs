using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_VE_VFECore : ModCompat
{
    public override string TargetAssemblyName => "VFECore";
    public override string DisplayName => "Vanilla Expanded Framework";

    [HarmonyPrefix]
    [HarmonyPatch("VFECore._TerrainGrid", "Postfix")]
    private static bool TerrainGrid_SetTerrain_Postfix()
    {
        return !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
