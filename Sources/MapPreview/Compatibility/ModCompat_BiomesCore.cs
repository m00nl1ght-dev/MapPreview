using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_BiomesCore : ModCompat
{
    public override string TargetAssemblyName => "BiomesCore";
    public override string DisplayName => "Biomes Core";

    protected override bool OnApply()
    {
        return AccessTools.Method("BiomesCore.Patches.WildPlantSpawner_GetBaseDesiredPlantsCountAt:UpdateCommonalityAt") != null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("BiomesCore.Patches.WildPlantSpawner_GetBaseDesiredPlantsCountAt", "UpdateCommonalityAt")]
    private static bool WildPlantSpawner_GetBaseDesiredPlantsCountAt_UpdateCommonalityAt()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
