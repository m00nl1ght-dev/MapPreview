using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(Map))]
internal static class RimWorld_Map
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
    
    [HarmonyPatch("FillComponents")]
    [HarmonyPrefix]
    private static bool FillComponents(Map __instance)
    {
        if (!ExactMapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        
        __instance.components.RemoveAll((Predicate<MapComponent>) (component => component == null));
        foreach (System.Type type in typeof (MapComponent).AllSubclassesNonAbstract())
        {
            if (__instance.GetComponent(type) == null)
            {
                if (IncludedMapComponents.Contains(type.FullName))
                {
                    try
                    {
                        __instance.components.Add((MapComponent) Activator.CreateInstance(type, (object) __instance));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not instantiate a MapComponent of type " + (object) type + ": " + (object) ex);
                    }
                }
            }
        }
        
        __instance.roadInfo = __instance.GetComponent<RoadInfo>();
        __instance.waterInfo = __instance.GetComponent<WaterInfo>();
        return false;
    }
}