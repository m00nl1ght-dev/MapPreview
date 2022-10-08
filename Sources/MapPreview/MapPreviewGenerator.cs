/*
 
Modified version of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/MapPreviewGenerator.cs

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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using LunarFramework.Patching;
using MapPreview.Patches;
using MapPreview.Promises;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

/// <summary>
/// Modified version of MapReroll.MapPreviewGenerator that uses full Map Generator mechanics for better mod compat.
/// </summary>
public class MapPreviewGenerator : IDisposable
{
    public static Map GeneratingMapOnCurrentThread => GeneratingPreviewMap.Value;
    public static bool IsGeneratingOnCurrentThread => GeneratingPreviewMap.Value != null;

    public static event Action OnPreviewThreadInit;
    public static event Action<MapPreviewRequest> OnBeginGenerating;
    public static event Action<MapPreviewResult> OnFinishedGenerating;
    
    private static readonly ThreadLocal<Map> GeneratingPreviewMap = new();
    
    public static MapPreviewGenerator Instance { get; private set; }
    
    private readonly Queue<MapPreviewRequest> _queuedRequests = new();
    private readonly Thread _workerThread;
    private EventWaitHandle _workHandle = new AutoResetEvent(false);
    private EventWaitHandle _disposeHandle = new AutoResetEvent(false);
    
    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(MapPreviewGenerator));

    private static FieldInfo _fieldMapGenData;

    private static readonly Color MissingTerrainColor = new(0.38f, 0.38f, 0.38f);
    private static readonly Color SolidStoneColor = GenColor.FromHex("36271C");
    private static readonly Color SolidStoneHighlightColor = GenColor.FromHex("4C3426");
    private static readonly Color SolidStoneShadowColor = GenColor.FromHex("1C130E");
    private static readonly Color CaveColor = GenColor.FromHex("42372b");

    private static readonly IReadOnlyCollection<string> IncludedGenStepDefs = new HashSet<string>
    {
        // Vanilla
        "ElevationFertility",
        "Caves",
        "Terrain",

        // CaveBiomes
        "CaveElevation",
        "CaveRiver",

        // TerraProjectCore
        "ElevationFertilityPost",
        "BetterCaves"
    };

    public static MapPreviewGenerator Init()
    {
        return Instance ??= new MapPreviewGenerator();
    }

    private MapPreviewGenerator()
    {
        _fieldMapGenData ??= AccessTools.Field(typeof(MapGenerator), "data");
        if (_fieldMapGenData == null) throw new Exception("Failed to reflect MapGenerator.data");
        
        _workerThread = new Thread(DoThreadWork);
        _workerThread.Start();
        
        MapPreviewAPI.LunarAPI.LifecycleHooks.DoOnceOnShutdown(Dispose);
    }

    public IPromise<MapPreviewResult> QueuePreviewRequest(MapPreviewRequest request)
    {
        if (_disposeHandle == null)
        {
            throw new Exception("ExactMapPreviewGenerator has already been disposed.");
        }
        
        if (request.MapSize.x > request.TextureSize.x || request.MapSize.z > request.TextureSize.z)
        {
            throw new Exception("Map size exceeds max preview size: " + request.MapSize + " > " + request.TextureSize);
        }
        
        MapPreviewAPI.SubscribeGenPatches(PatchGroupSubscriber);
        
        _queuedRequests.Enqueue(request);
        _workHandle.Set();
        
        return request.Promise;
    }

    private void DoThreadWork()
    {
        MapPreviewRequest request = null;
        try
        {
            OnPreviewThreadInit?.Invoke();

            var waitHandles = new WaitHandle[] { _workHandle, _disposeHandle };
            while (_queuedRequests.Count > 0 || WaitHandle.WaitAny(waitHandles) == 0)
            {
                Exception rejectException = null;
                if (_queuedRequests.Count > 0)
                {
                    request = _queuedRequests.Dequeue();

                    MapPreviewResult result = null;
                    try
                    {
                        OnBeginGenerating?.Invoke(request);
                        
                        result = new MapPreviewResult(request.MapTile, request.MapSize, request.TextureSize, request.ExistingBuffer);
                        GeneratePreview(request, result);

                        if (result.MapGenErrored)
                            throw new Exception("No terrain was generated for at least one map cell.");

                        OnFinishedGenerating?.Invoke(result);
                    }
                    catch (Exception e)
                    {
                        MapPreviewAPI.Logger.Error("Failed to generate map preview!", e);
                        MapPreviewAPI.Logger.Log("Map Info: \n" + PrintMapTileInfo(request.MapTile));
                        rejectException = e;
                    }
                    
                    if (_queuedRequests.Count == 0 && !MapPreviewAPI.IsGeneratingPreview)
                    {
                        MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
                    }

                    var promise = request.Promise;
                    MapPreviewAPI.LunarAPI.LifecycleHooks.DoOnce(() =>
                    {
                        if (rejectException == null)
                        {
                            promise.Resolve(result);
                        }
                        else
                        {
                            promise.Reject(rejectException);
                        }
                    });
                }
            }

            _workHandle.Close();
            _disposeHandle.Close();
            _disposeHandle = _workHandle = null;
        }
        catch (Exception e)
        {
            MapPreviewAPI.LunarAPI.LifecycleHooks.DoOnce(() =>
            {
                MapPreviewAPI.Logger.Error("Exception in preview generator thread!", e);
                request?.Promise.Reject(e);
            });
        }
    }

    public void Dispose()
    {
        if (Instance == this)
        {
            Instance = null;
            _queuedRequests.Clear();
            _disposeHandle.Set();
        }
    }

    public void ClearQueue()
    {
        _queuedRequests.Clear();
    }
    
    public void WaitForDisposal()
    {
        if (Instance == this || _workerThread is not { IsAlive: true } || _workerThread.ThreadState == ThreadState.WaitSleepJoin) return;
        _workerThread.Join(5 * 1000);
    }

    private static void GeneratePreview(MapPreviewRequest request, MapPreviewResult result)
    {
        var tickManager = Find.TickManager;
        var speedWas = tickManager?.CurTimeSpeed ?? TimeSpeed.Paused;
        var prevSeed = Find.World.info.seedString;

        try
        {
            MapPreviewAPI.IsGeneratingPreview = true;
            Find.World.info.seedString = request.Seed;

            Patch_RimWorld_GenStep_Terrain.SkipRiverFlowCalc = request.SkipRiverFlowCalc;
            
            tickManager?.Pause();

            var mapParent = new MapParent { Tile = request.MapTile, def = WorldObjectDefOf.Settlement};
            mapParent.SetFaction(Faction.OfPlayer);

            var mapSizeVec = new IntVec3(request.MapSize.x, 1, request.MapSize.z);
            GenerateMap(mapSizeVec, mapParent, MapGeneratorDefOf.Base_Player, result, request.UseTrueTerrainColors);

            AddBevelToSolidStone(result);
        }
        catch (Exception e)
        {
            MapPreviewAPI.Logger.Error("Error in preview generation!", e);
            MapPreviewAPI.Logger.Log("Map Info: \n" + PrintMapTileInfo(request.MapTile));
            result.MapGenErrored = true;
        }
        finally
        {
            MapPreviewAPI.IsGeneratingPreview = false;
            Find.World.info.seedString = prevSeed;

            Patch_RimWorld_GenStep_Terrain.SkipRiverFlowCalc = false;
            
            if (tickManager is { CurTimeSpeed: TimeSpeed.Paused })
            {
                tickManager.CurTimeSpeed = speedWas;
            }
        }
    }

    private static void GenerateMap(
        IntVec3 mapSize,
        MapParent parent,
        MapGeneratorDef mapGenerator,
        MapPreviewResult texture,
        bool useTrueTerrainColors)
    {
        var mapGeneratorData = (Dictionary<string, object>) _fieldMapGenData.GetValue(null);
        mapGeneratorData.Clear();

        var tickManager = Current.Game.tickManager;
        int startTick = tickManager.gameStartAbsTick;
        
        var map = new Map { generationTick = GenTicks.TicksGame };
        GeneratingPreviewMap.Value = map;
        
        int seed = Gen.HashCombineInt(Find.World.info.Seed, parent.Tile);
        Rand.PushState(seed);

        try
        {
            if (MapGenerator.mapBeingGenerated != null)
                throw new Exception("Attempted to generate map preview while another map is generating!");
            
            MapGenerator.mapBeingGenerated = map;
            if (startTick == 0) tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;

            map.info.Size = mapSize;
            map.info.parent = parent;
            map.ConstructComponents();

            var previewGenStep = new PreviewTextureGenStep(texture, useTrueTerrainColors);
            var previewGenStepDef = new GenStepDef { genStep = previewGenStep, order = 9999 };
            var genStepWithParamses = mapGenerator.genSteps
                .Where(d => IncludedGenStepDefs.Contains(d.defName))
                .Select(x => new GenStepWithParams(x, new GenStepParams()))
                .Append(new GenStepWithParams(previewGenStepDef, new GenStepParams()));
            
            MapGenerator.GenerateContentsIntoMap(genStepWithParamses, map, seed);
            
            map.weatherManager.EndAllSustainers();
            Find.SoundRoot.sustainerManager.EndAllInMap(map);
            Find.TickManager.RemoveAllFromMap(map);
            Find.Archive.Notify_MapRemoved(map);
        }
        finally
        {
            Rand.PopState();
            MapGenerator.mapBeingGenerated = null;
            if (startTick == 0) tickManager.gameStartAbsTick = 0;
            GeneratingPreviewMap.Value = null;
            mapGeneratorData.Clear();
        }
    }

    private static string PrintMapTileInfo(int tileId)
    {
        var worldGrid = Find.WorldGrid;
        if (worldGrid == null) return "No WorldGrid";

        var tile = worldGrid[tileId];
        if (tile == null) return "No Tile";
        
        var tickManager = Find.TickManager;

        var str = new StringBuilder();

        str.AppendLine("World Seed: " + Find.World?.info?.seedString);
        str.AppendLine("World Tile: " + tileId);
        str.AppendLine("Tick Speed: " + tickManager?.CurTimeSpeed);
        str.AppendLine("Biome: " + tile.biome?.defName);
        str.AppendLine("Biome TPMs: " + tile.biome?.terrainPatchMakers?.Count);
        str.AppendLine("Biome TTresh: " + tile.biome?.terrainsByFertility?.Count);
        str.AppendLine("Biome MCP: " + tile.biome?.modContentPack?.Name);
        str.AppendLine("River: " + tile.Rivers?.Count);
        str.AppendLine("Road: " + tile.Roads?.Count);

        return str.ToString();
    }

    private class PreviewTextureGenStep : GenStep
    {
        private readonly MapPreviewResult _texture;
        private readonly bool _useTrueTerrainColors;

        public PreviewTextureGenStep(MapPreviewResult texture, bool useTrueTerrainColors)
        {
            _texture = texture;
            _useTrueTerrainColors = useTrueTerrainColors;
        }

        public override int SeedPart => 0;

        public override void Generate(Map map, GenStepParams parms)
        {
            var mapBounds = CellRect.WholeMap(map);
            var colors = _useTrueTerrainColors ? TrueTerrainColors.TrueColors : TrueTerrainColors.DefaultColors;
            foreach (var cell in mapBounds)
            {
                var terrainDef = map.terrainGrid.TerrainAt(cell);

                Color pixelColor;
                if (terrainDef == null)
                {
                    pixelColor = Color.black;
                    _texture.MapGenErrored = true;
                } 
                else if (MapGenerator.Elevation[cell] >= 0.7 && !terrainDef.IsRiver)
                {
                    pixelColor = MapGenerator.Caves[cell] > 0 ? CaveColor : SolidStoneColor;
                }
                else if (!colors.TryGetValue(terrainDef.defName, out pixelColor))
                {
                    pixelColor = MissingTerrainColor;
                }

                _texture.SetPixel(cell.x, cell.z, pixelColor);
            }
        }
    }

    /// <summary>
    /// Adds highlights and shadows to the solid stone color in the texture
    /// </summary>
    private static void AddBevelToSolidStone(MapPreviewResult result)
    {
        for (int x = 0; x < result.MapSize.x; x++)
        {
            for (int z = 0; z < result.MapSize.z; z++)
            {
                var isStone = result.GetPixel(x, z) == SolidStoneColor;
                if (isStone)
                {
                    var colorBelow = z > 0 ? result.GetPixel(x, z - 1) : Color.clear;
                    var isStoneBelow = colorBelow == SolidStoneColor || colorBelow == SolidStoneHighlightColor ||
                                       colorBelow == SolidStoneShadowColor;
                    var isStoneAbove = z < result.MapSize.z - 1 && result.GetPixel(x, z + 1) == SolidStoneColor;
                    if (!isStoneAbove)
                    {
                        result.SetPixel(x, z, SolidStoneHighlightColor);
                    }
                    else if (!isStoneBelow)
                    {
                        result.SetPixel(x, z, SolidStoneShadowColor);
                    }
                }
            }
        }
    }
}