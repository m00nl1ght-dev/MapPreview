using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_VE_VEE : ModCompat
{
    public override string TargetAssemblyName => "VEE";
    public override string DisplayName => "Vanilla Events Expanded";

    protected override bool OnApply()
    {
        // This patch is no longer needed in VEE v2.x
        // Therefore it's conditional now and only applies to old versions of VEE
        return AccessTools.Method(TargetAssembly.GetType("VEE.FertilityGrid_Patch"), "Postfix") != null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("VEE.FertilityGrid_Patch", "Postfix")]
    private static bool FertilityGrid_Patch()
    {
        return !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
