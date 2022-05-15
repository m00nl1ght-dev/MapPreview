using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable All

namespace MapPreview.Patches;

/// <summary>
/// Makes World methods thread-safe.
/// </summary>
[HarmonyPatch]
internal static class RimWorld_World
{
    [HarmonyPatch(typeof(World), nameof(World.CoastDirectionAt))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool CoastDirectionAt(World __instance, int tileID, ref Rot4 __result)
    {
        if (!__instance.grid[tileID].biome.canBuildBase)
        {
            __result = Rot4.Invalid;
            return false;
        }

        var oceanDirs = new List<Rot4>(4);
        var neighbors = new List<int>(6);
        __instance.grid.GetTileNeighbors(tileID, neighbors);
        
        int index1 = 0;
        for (int count = neighbors.Count; index1 < count; ++index1)
        {
            if (__instance.grid[neighbors[index1]].biome == BiomeDefOf.Ocean)
            {
                Rot4 rotFromTo = __instance.grid.GetRotFromTo(tileID, neighbors[index1]);
                if (!oceanDirs.Contains(rotFromTo))
                    oceanDirs.Add(rotFromTo);
            }
        }
        
        if (oceanDirs.Count == 0)
        {
            __result = Rot4.Invalid;
            return false;
        }
        
        Rand.PushState();
        Rand.Seed = tileID;
        int index2 = Rand.Range(0, oceanDirs.Count);
        Rand.PopState();
        
        __result = oceanDirs[index2];
        return false;
    }
    
    [HarmonyPatch(typeof(World), nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool NaturalRockTypesIn(int tile, ref IEnumerable<ThingDef> __result, List<ThingDef> ___allNaturalRockDefs)
    {
        Rand.PushState();
        Rand.Seed = tile;
        
        if (___allNaturalRockDefs == null)
            ___allNaturalRockDefs = DefDatabase<ThingDef>.AllDefs.Where<ThingDef>((Func<ThingDef, bool>) (d => d.IsNonResourceNaturalRock)).ToList<ThingDef>();
        
        int num = Rand.RangeInclusive(2, 3);
        if (num > ___allNaturalRockDefs.Count) num = ___allNaturalRockDefs.Count;

        var naturalRockDefs = new List<ThingDef>((IEnumerable<ThingDef>) ___allNaturalRockDefs);
        var thingDefList = new List<ThingDef>();
        
        for (int index = 0; index < num; ++index)
        {
            ThingDef thingDef = naturalRockDefs.RandomElement<ThingDef>();
            naturalRockDefs.Remove(thingDef);
            thingDefList.Add(thingDef);
        }
        
        Rand.PopState();
        __result = (IEnumerable<ThingDef>) thingDefList;
        return false;
    }

    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.IsNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool IsNeighbor(int tile1, int tile2, ref bool __result, WorldGrid __instance)
    {
        var nb = new List<int>(6);
        __instance.GetTileNeighbors(tile1, nb);
        __result = nb.Contains(tile2);
        return false;
    }
    
    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.GetNeighborId))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool GetNeighborId(int tile1, int tile2, ref int __result, WorldGrid __instance)
    {
        var nb = new List<int>(6);
        __instance.GetTileNeighbors(tile1, nb);
        __result = nb.IndexOf(tile2);
        return false;
    }
    
    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.GetTileNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool GetTileNeighbor(int tileID, int adjacentId, ref int __result, WorldGrid __instance)
    {
        var nb = new List<int>(6);
        __instance.GetTileNeighbors(tileID, nb);
        __result = nb[adjacentId];
        return false;
    }
}