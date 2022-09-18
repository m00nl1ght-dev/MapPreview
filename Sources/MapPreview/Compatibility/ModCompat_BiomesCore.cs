using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

[HarmonyPatch]
internal class ModCompat_BiomesCore : ModCompat
{
    public override string TargetAssemblyName => "BiomesCore";
    public override string DisplayName => "Biomes Core";

    [HarmonyPrefix]
    [HarmonyPatch("BiomesCore.Patches.WildPlantSpawner_GetBaseDesiredPlantsCountAt", "UpdateCommonalityAt")]
    private static bool WildPlantSpawner_GetBaseDesiredPlantsCountAt_UpdateCommonalityAt()
    {
        return !Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}