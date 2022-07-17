using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MapPreview.Patches;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantAssignment

[StaticConstructorOnStartup]
internal static class ModCompat_GeologicalLandforms
{
    public static bool IsPresent { get; }
    
    static ModCompat_GeologicalLandforms()
    {
        try
        {
            var gpType = GenTypes.GetTypeInAnyAssembly("GeologicalLandforms.GraphEditor.NodeTerrainGridPreview");
            if (gpType != null)
            {
                Harmony harmony = new("Map Preview Geological Landforms Integration");
                
                var wiType = GenTypes.GetTypeInAnyAssembly("GeologicalLandforms.WorldTileInfo");

                var gpMethod = AccessTools.Method(gpType, "GetTerrainColor");
                var wiMethod = AccessTools.Method(wiType, "InvalidateCache");
                if (gpMethod == null || wiMethod == null) throw new Exception("methods not found");

                var self = typeof(ModCompat_GeologicalLandforms);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod gpPrefix = new(self.GetMethod(nameof(NodeTerrainGridPreview_GetTerrainColor), bindingFlags));
                HarmonyMethod wiPostfix = new(self.GetMethod(nameof(WorldTileInfo_InvalidateCache), bindingFlags));

                harmony.Patch(gpMethod, gpPrefix);
                harmony.Patch(wiMethod, null, wiPostfix);
                IsPresent = true;
            }
        }
        catch (Exception e)
        {
            Log.Warning(ModInstance.LogPrefix + "Failed to apply optional integration patches for Geological Landforms!");
            Debug.LogException(e);
        }
    }
    
    private static bool NodeTerrainGridPreview_GetTerrainColor(TerrainDef def, ref Color __result)
    {
        return !TrueTerrainColors.CurrentTerrainColors.TryGetValue(def.defName, out __result);
    }
    
    private static void WorldTileInfo_InvalidateCache()
    {
        RimWorld_WorldInterface.Refresh();
    }
}