using System;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public static class MapSizeUtility
{
    public static IntVec2 MinMapSize = new(10, 10);
    public static IntVec2 MaxMapSize = new(500, 500);

    public static Func<IntVec2> MapSizeOverride;

    public static IntVec2 DetermineMapSize(World world, MapParent mapParent)
    {
        var mapSize = DetermineMapSizeUnclamped(world, mapParent);
        var sizeX = Mathf.Clamp(mapSize.x, MinMapSize.x, MaxMapSize.x);
        var sizeZ = Mathf.Clamp(mapSize.z, MinMapSize.z, MaxMapSize.z);
        return new IntVec2(sizeX, sizeZ);
    }

    public static IntVec2 DetermineMapSizeUnclamped(World world, MapParent mapParent)
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

        if (MapSizeOverride != null)
        {
            var fromOverride = MapSizeOverride.Invoke();
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
