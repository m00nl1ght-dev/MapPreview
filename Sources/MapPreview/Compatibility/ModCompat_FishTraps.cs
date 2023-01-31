using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_FishTraps : ModCompat
{
    public override string TargetAssemblyName => "FishTraps";
    public override string DisplayName => "Fish Traps";

    [HarmonyPrefix]
    [HarmonyPatch("FishTraps.HarmonyPatches.TerrainChangePatch", "Postfix")]
    private static bool TerrainChangePatch_Postfix()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
