using System;
using System.Collections.Generic;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Gen")]
[HarmonyPatch(typeof(Map))]
internal static class Patch_Verse_Map
{
    private static readonly HashSet<string> IncludedMapComponents = new HashSet<string>
    {
        // Vanilla
        typeof(RoadInfo).FullName,
        typeof(WaterInfo).FullName,
        
        // Water Freezes
        "ActiveTerrain.SpecialTerrainList",
        
        // VFE Core
        "VFECore.SpecialTerrainList",
        
        // Alpha Biomes
        "AlphaBiomes.MapComponentExtender",
        
        // Dubs Bad Hygiene
        "DubsBadHygiene.MapComponent_Hygiene",
        
        // Dubs Paint Shop
        "DubRoss.MapComponent_PaintShop"
    };
    
    [HarmonyPrefix]
    [HarmonyPatch("FillComponents")]
    private static bool FillComponents(Map __instance)
    {
        if (!Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        
        __instance.components.RemoveAll(component => component == null);
        foreach (var type in typeof (MapComponent).AllSubclassesNonAbstract())
        {
            if (__instance.GetComponent(type) == null)
            {
                if (IncludedMapComponents.Contains(type.FullName))
                {
                    try
                    {
                        __instance.components.Add((MapComponent) Activator.CreateInstance(type, __instance));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not instantiate a MapComponent of type " + type + ": " + ex);
                    }
                }
            }
        }
        
        __instance.roadInfo = __instance.GetComponent<RoadInfo>();
        __instance.waterInfo = __instance.GetComponent<WaterInfo>();
        return false;
    }
}