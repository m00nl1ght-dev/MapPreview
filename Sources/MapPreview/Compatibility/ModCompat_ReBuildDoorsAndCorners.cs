using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

/// <summary>
/// Skip roof cache rebuild on roof change on preview maps.
/// </summary>
[HarmonyPatch]
internal class ModCompat_ReBuildDoorsAndCorners : ModCompat
{
    public override string TargetAssemblyName => "ReBuildDoorsAndCorners";
    public override string DisplayName => "ReBuild Doors And Corners";

    [HarmonyPrefix]
    [HarmonyPatch("ReBuildDoorsAndCorners.RoofGrid_SetRoof_Patch", "Postfix")]
    private static bool RoofGrid_SetRoof_Patch_Postfix()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
