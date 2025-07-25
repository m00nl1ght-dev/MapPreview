/*

Modified part of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/MapPreviewGenerator.cs

MIT License

Copyright (c) 2017 UnlimitedHugs, modifications (c) 2022 m00nl1ght <https://github.com/m00nl1ght-dev>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MapPreview.Promises;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewRequest
{
    public Promise<MapPreviewResult> Promise { get; }

    public int Seed { get; }

    #if RW_1_6_OR_GREATER
    public PlanetTile MapTile { get; }
    #else
    public int MapTile { get; }
    #endif

    public IntVec2 MapSize { get; }

    public IntVec2 TextureSize { get; set; }
    public Color[] ExistingBuffer { get; set; }

    public bool UseTrueTerrainColors { get; set; }
    public bool UseMinimalMapComponents { get; set; }

    #if !RW_1_6_OR_GREATER
    public bool SkipRiverFlowCalc { get; set; } = true;
    #endif

    public MapGeneratorDef GeneratorDef { get; set; } = MapGeneratorDefOf.Base_Player;
    public Predicate<GenStepDef> GenStepFilter { get; set; } = DefaultGenStepFilter;

    public readonly Stopwatch Timer = new();

    public bool Pending => Promise?.CurState == PromiseState.Pending;

    public MapPreviewRequest(string worldSeed, int mapTile, IntVec2 mapSize) :
        this(SeedFromWorldSeed(worldSeed, mapTile), mapTile, mapSize) { }

    #if RW_1_6_OR_GREATER
    public MapPreviewRequest(int seed, PlanetTile mapTile, IntVec2 mapSize)
    #else
    public MapPreviewRequest(int seed, int mapTile, IntVec2 mapSize)
    #endif
    {
        Promise = new Promise<MapPreviewResult>();
        Seed = seed;
        MapTile = mapTile;
        MapSize = mapSize;
        TextureSize = MapSize;
    }

    private static int SeedFromWorldSeed(string worldSeed, int mapTile)
    {
        var world = Find.World;

        if (world != null && world.info.seedString == worldSeed)
            return SeedRerollData.GetMapSeed(world, mapTile);

        return Gen.HashCombineInt(GenText.StableStringHash(worldSeed), mapTile);
    }

    public static void AddDefaultGenStepPredicate(Predicate<GenStepDef> predicate) => DefaultGenStepPredicates.Add(predicate);

    public static readonly Predicate<GenStepDef> DefaultGenStepFilter = genStep => DefaultGenStepPredicates.Any(p => p(genStep));

    private static readonly List<Predicate<GenStepDef>> DefaultGenStepPredicates = new()
    {
        genStep => DefaultGenSteps.Contains(genStep.defName)
    };

    private static readonly List<string> DefaultGenSteps = new()
    {
        "ElevationFertility",
        "Terrain",
        "MutatorPostElevationFertility",
        "MutatorPostTerrain",
        "Space",
        "AsteroidBasic",
    };
}
