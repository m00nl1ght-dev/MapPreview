using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace MapPreview;

public class SeedRerollData : WorldComponent
{
    private Dictionary<int, int> _mapSeeds = new();

    public SeedRerollData(World world) : base(world) { }

    public static bool IsMapSeedRerolled(World world, int tile, out int seed)
    {
        var seedRerollData = world.GetComponent<SeedRerollData>();

        if (seedRerollData != null && seedRerollData.TryGet(tile, out var savedSeed))
        {
            seed = savedSeed;
            return true;
        }

        seed = GetOriginalMapSeed(world, tile);
        return false;
    }

    public static int GetMapSeed(World world, int tile)
    {
        var seedRerollData = world.GetComponent<SeedRerollData>();
        if (seedRerollData != null && seedRerollData.TryGet(tile, out var savedSeed)) return savedSeed;
        return GetOriginalMapSeed(world, tile);
    }

    public static int GetOriginalMapSeed(World world, int tile)
    {
        return Gen.HashCombineInt(world.info.Seed, tile);
    }

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
