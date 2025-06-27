using System;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Map))]
internal static class Patch_Verse_Map
{
    [HarmonyPrefix]
    [HarmonyPatch("FillComponents")]
    private static bool FillComponents_Prefix(Map __instance)
    {
        if (!MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;

        foreach (var type in typeof(MapComponent).AllSubclassesNonAbstract())
        {
            if (MapPreviewGenerator.IncludedMapComponentsFull.Contains(type.FullName))
            {
                try
                {
                    __instance.components.Add((MapComponent) Activator.CreateInstance(type, __instance));
                }
                catch (Exception e)
                {
                    MapPreviewAPI.Logger.Error($"Failed to instantiate component {type} for preview map", e);
                }
            }
        }

        __instance.roadInfo = __instance.GetComponent<RoadInfo>();
        __instance.waterInfo = __instance.GetComponent<WaterInfo>();

        return false;
    }

    #if RW_1_6_OR_GREATER

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Map.ConstructComponents))]
    private static void ConstructComponents_Postfix(Map __instance)
    {
        if (MapPreviewAPI.IsGeneratingPreview && MapPreviewGenerator.IsGeneratingOnCurrentThread) return;

        const uint ExpectedIterations = 1U; // GasGrid ctor does one Rand call

        if (MapGenerator.mapBeingGenerated == __instance && Rand.iterations != ExpectedIterations)
        {
            MapPreviewAPI.Logger.Warn($"Map previews may not be accurate: Unexpected Rand iteration count {Rand.iterations}");
        }
    }

    #endif

    #if DEBUG

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Map.FinalizeInit))]
    private static void FinalizeInit_Postfix()
    {
        var cameraDriverConfig = Find.CameraDriver.config;
        cameraDriverConfig.sizeRange.max = 200f;
        cameraDriverConfig.zoomSpeed = 5f;

        Find.PlaySettings.showWorldFeatures = false;
    }

    #endif
}
