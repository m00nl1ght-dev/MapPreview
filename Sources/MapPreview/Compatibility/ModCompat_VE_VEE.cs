using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_VE_VEE : ModCompat
{
    public override string TargetAssemblyName => "VEE";
    public override string DisplayName => "Vanilla Events Expanded";

    [HarmonyPrefix]
    [HarmonyPatch("VEE.FertilityGrid_Patch", "Postfix")]
    private static bool FertilityGrid_Patch()
    {
        return !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
