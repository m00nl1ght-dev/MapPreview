using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_TerraProjectCore : ModCompat
{
    public override string TargetAssemblyName => "TerraCore";

    protected override bool OnApply()
    {
        var mcp = ModContentPack;
        MapPreviewRequest.AddDefaultGenStepPredicate(def => def.modContentPack == mcp && DefNames.Contains(def.defName));
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("TerraCore.GenRoof", "SetRoofComplete")]
    private static bool GenRoof_SetRoofComplete()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }

    [HarmonyPrefix]
    [HarmonyPatch("TerraCore.GenRoof", "SetStableDeepRoof")]
    private static bool GenRoof_SetStableDeepRoof()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }

    private static readonly List<string> DefNames = new()
    {
        "ElevationFertilityPost", "BetterCaves"
    };
}
