using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_AnimalTraps : ModCompat
{
    public override string TargetAssemblyName => "AnimalTrap";
    public override string DisplayName => "Animal Traps";

    [HarmonyPrefix]
    [HarmonyPatch("AnimalTraps.HarmonyPatches.TerrainChangePatch", "Postfix")]
    private static bool TerrainChangePatch_Postfix()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
