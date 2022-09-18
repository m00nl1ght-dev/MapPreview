using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

[HarmonyPatch]
internal class ModCompat_AnimalTraps : ModCompat
{
    public override string TargetAssemblyName => "AnimalTrap";
    public override string DisplayName => "Animal Traps";

    [HarmonyPrefix]
    [HarmonyPatch("AnimalTraps.HarmonyPatches.TerrainChangePatch", "Postfix")]
    private static bool TerrainChangePatch_Postfix()
    {
        return !Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}