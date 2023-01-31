using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_SaveOurShip : ModCompat
{
    public override string TargetAssemblyName => "ShipsHaveInsides";
    public override string DisplayName => "Save Our Ship 2";

    [HarmonyPostfix]
    [HarmonyPriority(-1000)]
    [HarmonyPatch(typeof(Map))]
    [HarmonyPatch("Biome", MethodType.Getter)]
    private static void Biome_Getter(Map __instance, ref BiomeDef __result)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread)
        {
            __result = __instance.TileInfo.biome;
        }
    }
}
