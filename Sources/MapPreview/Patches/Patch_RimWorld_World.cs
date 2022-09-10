using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

/// <summary>
/// Holds a separate state for World caches on the preview thread.
/// All patches are conditional and are only active while a preview is actually generating.
/// </summary>
[PatchGroup("Gen")]
[HarmonyPatch(typeof(World))]
internal static class Patch_RimWorld_World
{
    private static readonly List<ThingDef> tmpNaturalRockDefs = new();
    private static readonly List<int> tmpNeighbors = new();
    private static readonly List<Rot4> tmpOceanDirs = new();

    [HarmonyPatch(nameof(World.CoastDirectionAt))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool CoastDirectionAt(World __instance, int tileID, ref Rot4 __result)
    {
        if (!Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        
        var grid = __instance.grid;
        if (!grid[tileID].biome.canBuildBase)
        {
            __result = Rot4.Invalid;
            return false;
        }

        tmpOceanDirs.Clear();
        grid.GetTileNeighbors(tileID, tmpNeighbors);
        
        int index1 = 0;
        for (int count = tmpNeighbors.Count; index1 < count; ++index1)
        {
            if (grid[tmpNeighbors[index1]].biome == BiomeDefOf.Ocean)
            {
                var rotFromTo = grid.GetRotFromTo(tileID, tmpNeighbors[index1]);
                if (!tmpOceanDirs.Contains(rotFromTo)) tmpOceanDirs.Add(rotFromTo);
            }
        }
        
        if (tmpOceanDirs.Count == 0)
        {
            __result = Rot4.Invalid;
            return false;
        }

        Rand.PushState(tileID);
        int index2 = Rand.Range(0, tmpOceanDirs.Count);
        Rand.PopState();
        
        __result = tmpOceanDirs[index2];
        return false;
    }
    
    [HarmonyPatch(nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool NaturalRockTypesIn(int tile, ref IEnumerable<ThingDef> __result, ref List<ThingDef> ___allNaturalRockDefs)
    {
        if (!Main.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread) return true;
        
        Rand.PushState(tile);
        
        ___allNaturalRockDefs ??= DefDatabase<ThingDef>.AllDefs.Where(d => d.IsNonResourceNaturalRock).ToList();
        
        int num = Rand.RangeInclusive(2, 3);
        if (num > ___allNaturalRockDefs.Count)
            num = ___allNaturalRockDefs.Count;
        
        tmpNaturalRockDefs.Clear();
        tmpNaturalRockDefs.AddRange(___allNaturalRockDefs);
        
        var thingDefList = new List<ThingDef>();
        for (int index = 0; index < num; ++index)
        {
            var thingDef = tmpNaturalRockDefs.RandomElement();
            tmpNaturalRockDefs.Remove(thingDef);
            thingDefList.Add(thingDef);
        }
        
        Rand.PopState();
        
        __result = thingDefList;
        return false;
    }
}