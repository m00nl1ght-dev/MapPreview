#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_PocketMap_RV : ModCompat
{
    public override string TargetAssemblyName => "VehicleFrameworkFix";
    public override string DisplayName => "RV with built-in PD";

    [HarmonyPrefix]
    [HarmonyPatch("flammpfeil.VehicleFrameworkFix.GenerateContentsIntoMap_Patch", "Prefix")]
    private static bool GenerateContentsIntoMap_Patch()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}

#endif
