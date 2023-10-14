using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("GenLow")]
[HarmonyPatch(typeof(TerrainGrid))]
internal static class Patch_Verse_TerrainGrid
{
    [HarmonyPrefix]
    [HarmonyPatch("SetTerrain")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool SetTerrain(TerrainGrid __instance, Map ___map, IntVec3 c, TerrainDef newTerr)
    {
        if (MapPreviewGenerator.IsGeneratingOnCurrentThread)
        {
            if (newTerr != null)
            {
                int index = ___map.cellIndices.CellToIndex(c);
                __instance.topGrid[index] = newTerr;
                __instance.colorGrid[index] = null;
            }

            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetTerrainColor")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool SetTerrainColor(TerrainGrid __instance, Map ___map, IntVec3 c, ColorDef color)
    {
        if (MapPreviewGenerator.IsGeneratingOnCurrentThread)
        {
            int index = ___map.cellIndices.CellToIndex(c);
            __instance.colorGrid[index] = color;
            return false;
        }
        
        return true;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("RemoveTopLayer")]
    [HarmonyPriority(Priority.VeryHigh)]
    private static bool RemoveTopLayer()
    {
        return !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
