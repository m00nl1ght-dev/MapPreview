using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace MapPreview;

public class SeedRerollData : WorldComponent
{
    private Dictionary<int, int> _mapSeeds = new();

    public SeedRerollData(World world) : base(world) { }

    public static SeedRerollData GetFor(World world)
    {
        var seedRerollData = world.GetComponent<SeedRerollData>();

        if (seedRerollData == null)
        {
            MapPreviewAPI.Logger.Warn("This world is missing the SeedRerollData component, adding it.");
            seedRerollData = new SeedRerollData(world);
            world.components.Add(seedRerollData);
        }

        return seedRerollData;
    }

    #if RW_1_6_OR_GREATER

    public static bool IsMapSeedRerolled(World world, PlanetTile tile, out int seed)
    {
        if (SupportsTile(tile) && GetFor(world).TryGet(tile, out seed)) return true;
        seed = GetOriginalMapSeed(world, tile);
        return false;
    }

    public static int GetMapSeed(World world, PlanetTile tile)
    {
        return SupportsTile(tile) && GetFor(world).TryGet(tile, out var seed) ? seed : GetOriginalMapSeed(world, tile);
    }

    public static int GetOriginalMapSeed(World world, PlanetTile tile)
    {
        return Gen.HashCombineInt(world.info.Seed, tile.GetHashCode());
    }

    public static bool SupportsTile(PlanetTile tile)
    {
        return tile.Layer.LayerID == 0 && tile.tileId >= 0;
    }

    #else

    public static bool IsMapSeedRerolled(World world, int tile, out int seed)
    {
        if (SupportsTile(tile) && GetFor(world).TryGet(tile, out seed)) return true;
        seed = GetOriginalMapSeed(world, tile);
        return false;
    }

    public static int GetMapSeed(World world, int tile)
    {
        return SupportsTile(tile) && GetFor(world).TryGet(tile, out var seed) ? seed : GetOriginalMapSeed(world, tile);
    }

    public static int GetOriginalMapSeed(World world, int tile)
    {
        return Gen.HashCombineInt(world.info.Seed, tile);
    }

    public static bool SupportsTile(int tile)
    {
        return tile >= 0;
    }

    #endif

    public bool TryGet(int tileId, out int seed)
    {
        return _mapSeeds.TryGetValue(tileId, out seed);
    }

    public void Commit(int tileId, int seed) => Commit(tileId, seed, true);

    public void Commit(int tileId, int seed, bool notifyWorldChanged)
    {
        _mapSeeds[tileId] = seed;
        if (notifyWorldChanged) MapPreviewAPI.NotifyWorldChanged();
    }

    public void Reset(int tileId) => Reset(tileId, true);

    public void Reset(int tileId, bool notifyWorldChanged)
    {
        _mapSeeds.Remove(tileId);
        if (notifyWorldChanged) MapPreviewAPI.NotifyWorldChanged();
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref _mapSeeds, "mapSeeds", LookMode.Value, LookMode.Value);
    }
}
