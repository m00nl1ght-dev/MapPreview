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

public static class ModCompat_GeologicalLandforms
{
    public static bool IsPresent { get; private set; }
    
    public static void Apply()
    {
        try
        {
            var gpType = GenTypes.GetTypeInAnyAssembly("GeologicalLandforms.GraphEditor.NodeTerrainGridPreview");
            if (gpType != null)
            {
                Harmony harmony = new("Map Preview Geological Landforms Integration");

                var gpMethod = AccessTools.Method(gpType, "GetTerrainColor");
                if (gpMethod == null) return;

                var self = typeof(ModCompat_GeologicalLandforms);
                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

                HarmonyMethod prefix = new(self.GetMethod(nameof(NodeTerrainGridPreview_GetTerrainColor), bindingFlags));

                harmony.Patch(gpMethod, prefix);
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
}