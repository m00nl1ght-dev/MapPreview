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
    [ThreadStatic]
    private static List<ThingDef> _tsc_naturalRockDefs;

    [ThreadStatic]
    private static List<Rot4> _tsc_oceanDirs;
    
    [ThreadStatic]
    private static List<int> _tsc_neighbors;

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

        _tsc_oceanDirs ??= new List<Rot4>(4);
        _tsc_neighbors ??= new List<int>(6);
        
        __instance.grid.GetTileNeighbors(tileID, _tsc_neighbors);
        
        int index1 = 0;
        _tsc_oceanDirs.Clear();
        for (int count = _tsc_neighbors.Count; index1 < count; ++index1)
        {
            if (__instance.grid[_tsc_neighbors[index1]].biome == BiomeDefOf.Ocean)
            {
                Rot4 rotFromTo = __instance.grid.GetRotFromTo(tileID, _tsc_neighbors[index1]);
                if (!_tsc_oceanDirs.Contains(rotFromTo))
                    _tsc_oceanDirs.Add(rotFromTo);
            }
        }
        
        if (_tsc_oceanDirs.Count == 0)
        {
            __result = Rot4.Invalid;
            return false;
        }
        
        Rand.PushState();
        Rand.Seed = tileID;
        int index2 = Rand.Range(0, _tsc_oceanDirs.Count);
        Rand.PopState();
        
        __result = _tsc_oceanDirs[index2];
        return false;
    }
    
    [HarmonyPatch(typeof(World), nameof(World.NaturalRockTypesIn))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool NaturalRockTypesIn(int tile, ref IEnumerable<ThingDef> __result, ref List<ThingDef> ___allNaturalRockDefs)
    {
        Rand.PushState();
        Rand.Seed = tile;
        
        if (___allNaturalRockDefs == null)
            ___allNaturalRockDefs = DefDatabase<ThingDef>.AllDefs.Where<ThingDef>((Func<ThingDef, bool>) (d => d.IsNonResourceNaturalRock)).ToList<ThingDef>();
        
        int num = Rand.RangeInclusive(2, 3);
        if (num > ___allNaturalRockDefs.Count) num = ___allNaturalRockDefs.Count;

        _tsc_naturalRockDefs ??= new List<ThingDef>(___allNaturalRockDefs.Count);
        
        _tsc_naturalRockDefs.Clear();
        _tsc_naturalRockDefs.AddRange(___allNaturalRockDefs);
        
        var thingDefList = new List<ThingDef>();
        
        for (int index = 0; index < num; ++index)
        {
            ThingDef thingDef = _tsc_naturalRockDefs.RandomElement<ThingDef>();
            _tsc_naturalRockDefs.Remove(thingDef);
            thingDefList.Add(thingDef);
        }
        
        Rand.PopState();
        __result = thingDefList;
        return false;
    }

    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.IsNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool IsNeighbor(int tile1, int tile2, ref bool __result, WorldGrid __instance)
    {
        _tsc_neighbors ??= new List<int>(6);
        __instance.GetTileNeighbors(tile1, _tsc_neighbors);
        __result = _tsc_neighbors.Contains(tile2);
        return false;
    }
    
    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.GetNeighborId))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool GetNeighborId(int tile1, int tile2, ref int __result, WorldGrid __instance)
    {
        _tsc_neighbors ??= new List<int>(6);
        __instance.GetTileNeighbors(tile1, _tsc_neighbors);
        __result = _tsc_neighbors.IndexOf(tile2);
        return false;
    }
    
    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.GetTileNeighbor))]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPrefix]
    private static bool GetTileNeighbor(int tileID, int adjacentId, ref int __result, WorldGrid __instance)
    {
        _tsc_neighbors ??= new List<int>(6);
        __instance.GetTileNeighbors(tileID, _tsc_neighbors);
        __result = _tsc_neighbors[adjacentId];
        return false;
    }
}