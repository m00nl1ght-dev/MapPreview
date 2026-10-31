using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public static class MapSizeUtility
{
    public static IntVec2 MinMapSize = new(10, 10);
    public static IntVec2 MaxMapSize = new(500, 500);

    public static Func<IntVec2> MapSizeOverride;
    public static Func<IntVec2> GameInitMapSizeOverride;

    #if RW_1_6_OR_GREATER

    public delegate IntVec2 MapSizeTransform(World world, PlanetTile tile, IntVec2 size);

    public static readonly List<MapSizeTransform> MapSizeTransforms = [];

    public static IntVec2 DetermineMapSize(World world, MapParent mapParent)
    {
        return DetermineMapSize(world, PlanetTile.Invalid, mapParent);
    }

    #endif

    #if RW_1_6_OR_GREATER
    public static IntVec2 DetermineMapSize(World world, PlanetTile tile, MapParent mapParent)
    #else
    public static IntVec2 DetermineMapSize(World world, MapParent mapParent)
    #endif
    {
        var mapSize = DetermineMapSizeRaw(world, mapParent);

        #if RW_1_6_OR_GREATER

        if (tile.Valid)
        {
            mapSize = MapSizeTransforms.Aggregate(mapSize, (v, func) => func(world, tile, v));
        }

        #endif

        var sizeX = Mathf.Clamp(mapSize.x, MinMapSize.x, MaxMapSize.x);
        var sizeZ = Mathf.Clamp(mapSize.z, MinMapSize.z, MaxMapSize.z);

        return new IntVec2(sizeX, sizeZ);
    }

    public static IntVec2 DetermineMapSizeRaw(World world, MapParent mapParent)
    {
        if (mapParent is Site site)
        {
            var fromSite = site.PreferredMapSize;
            return new IntVec2(fromSite.x, fromSite.z);
        }

        if (Current.ProgramState != ProgramState.Entry)
        {
            var fromWorld = world.info.initialMapSize;
            return new IntVec2(fromWorld.x, fromWorld.z);
        }

        var mapSizeOverride = GameInitMapSizeOverride ?? MapSizeOverride;
        if (mapSizeOverride != null)
        {
            var fromOverride = mapSizeOverride.Invoke();
            if (fromOverride is { x: > 0, z: > 0 })
            {
                return new IntVec2(fromOverride.x, fromOverride.z);
            }
        }

        var gameInitData = Find.GameInitData;
        if (gameInitData != null)
        {
            return new IntVec2(gameInitData.mapSize, gameInitData.mapSize);
        }

        return new IntVec2(250, 250);
    }
}
