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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
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

    public static MapPreviewRequest CurrentRequest { get; private set; }

    public static MapPreviewGenerator Instance { get; private set; }

    private readonly ConcurrentQueue<MapPreviewRequest> _queuedRequests = new();
    private readonly Thread _workerThread;
    
    private EventWaitHandle _workHandle = new AutoResetEvent(false);
    private EventWaitHandle _disposeHandle = new ManualResetEvent(false);
    private EventWaitHandle _idleHandle = new ManualResetEvent(true);

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(MapPreviewGenerator));

    private static readonly Color MissingTerrainColor = new(0.38f, 0.38f, 0.38f);
    private static readonly Color SolidStoneColor = GenColor.FromHex("36271C");
    private static readonly Color SolidStoneHighlightColor = GenColor.FromHex("4C3426");
    private static readonly Color SolidStoneShadowColor = GenColor.FromHex("1C130E");
    private static readonly Color CaveColor = GenColor.FromHex("42372b");

    public static MapPreviewGenerator Init()
    {
        return Instance ??= new MapPreviewGenerator();
    }

    private MapPreviewGenerator()
    {
        _workerThread = new Thread(DoThreadWork);
        _workerThread.Start();

        MapPreviewAPI.LunarAPI.LifecycleHooks.DoOnceOnShutdown(Dispose);
    }

    public IPromise<MapPreviewResult> QueuePreviewRequest(MapPreviewRequest request)
    {
        if (_disposeHandle == null)
        {
            throw new Exception("MapPreviewGenerator has already been disposed.");
        }

        if (request.MapSize.x > request.TextureSize.x || request.MapSize.z > request.TextureSize.z)
        {
            throw new Exception("Map size exceeds max preview size: " + request.MapSize + " > " + request.TextureSize);
        }

        MapPreviewAPI.SubscribeGenPatches(PatchGroupSubscriber);

        _queuedRequests.Enqueue(request);

        _idleHandle.Reset();
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
                if (_queuedRequests.TryDequeue(out request))
                {
                    CurrentRequest = request;

                    MapPreviewResult result = null;
                    try
                    {
                        OnBeginGenerating?.Invoke(request);

                        result = new MapPreviewResult(request);
                        GeneratePreview(request, result);

                        if (result.InvalidCells > 0)
                            throw new Exception($"No valid terrain was generated for {result.InvalidCells} map cells");

                        OnFinishedGenerating?.Invoke(result);
                    }
                    catch (Exception e)
                    {
                        rejectException = e;
                    }
                    finally
                    {
                        CurrentRequest = null;
                    }

                    var promise = request.Promise;
                    var mapTile = request.MapTile;

                    MapPreviewAPI.LunarAPI.LifecycleHooks.DoOnce(() =>
                    {
                        if (rejectException == null)
                        {
                            promise.Resolve(result);
                        }
                        else
                        {
                            MapPreviewAPI.Logger.Error("Failed to generate map preview!", rejectException);
                            MapPreviewAPI.Logger.Log("Map Info: \n" + PrintMapTileInfo(mapTile));
                            promise.Reject(rejectException);
                        }

                        if (_queuedRequests.Count == 0 && !MapPreviewAPI.IsGeneratingPreview)
                        {
                            MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
                        }
                    });

                    if (_queuedRequests.Count == 0)
                    {
                        _idleHandle.Set();
                    }
                }
            }
        }
        catch (Exception e)
        {
            MapPreviewAPI.LunarAPI.LifecycleHooks.DoOnce(() =>
            {
                MapPreviewAPI.Logger.Error("Exception in preview generator thread!", e);
                request?.Promise.Reject(e);
            });
        }
        finally
        {
            var idleHandle = _idleHandle;
            var workHandle = _workHandle;
            var disposeHandle = _disposeHandle;

            _idleHandle = _disposeHandle = _workHandle = null;

            idleHandle.Set();
            idleHandle.Close();
            workHandle.Close();
            disposeHandle.Close();
        }
    }

    public bool Abort()
    {
        try
        {
            Dispose();
            _workerThread.Abort();
            _workerThread.Join(3 * 1000);
            return true;
        }
        catch (Exception e)
        {
            MapPreviewAPI.Logger.Warn("Failed to abort preview thread", e);
            return false;
        }
    }

    public void Dispose()
    {
        if (Instance == this)
        {
            Instance = null;
            ClearQueue();
            _disposeHandle.Set();
        }
    }

    public void ClearQueue()
    {
        while (_queuedRequests.Count > 0)
        {
            if (!_queuedRequests.TryDequeue(out _)) break;
        }
    }

    public void WaitForDisposal(int timeout = 5)
    {
        if (Instance == this || _workerThread is not { IsAlive: true } || _workerThread.ThreadState == ThreadState.WaitSleepJoin) return;
        _workerThread.Join(timeout * 1000);
    }

    public bool WaitUntilIdle(int timeout = 30)
    {
        var idleHandle = _idleHandle;
        if (idleHandle == null || _workerThread is not { IsAlive: true } || _workerThread.ThreadState == ThreadState.WaitSleepJoin) return true;
        return idleHandle.WaitOne(timeout * 1000);
    }

    private static void GeneratePreview(MapPreviewRequest request, MapPreviewResult result)
    {
        var tickManager = Find.TickManager;
        var speedWas = tickManager?.CurTimeSpeed ?? TimeSpeed.Paused;

        try
        {
            MapPreviewAPI.IsGeneratingPreview = true;

            Patch_RimWorld_GenStep_Terrain.SkipRiverFlowCalc = request.SkipRiverFlowCalc;

            tickManager?.Pause();

            request.Timer.Start();

            GenerateContentsIntoPreview(request, result);

            AddBevelToSolidStone(result);

            request.Timer.Stop();
        }
        finally
        {
            MapPreviewAPI.IsGeneratingPreview = false;

            Patch_RimWorld_GenStep_Terrain.SkipRiverFlowCalc = false;

            if (tickManager is { CurTimeSpeed: TimeSpeed.Paused })
            {
                tickManager.CurTimeSpeed = speedWas;
            }
        }
    }

    private static void GenerateContentsIntoPreview(MapPreviewRequest request, MapPreviewResult result)
    {
        if (MapGenerator.mapBeingGenerated != null)
            throw new Exception("Attempted to generate map preview while another map is generating!");
        
        MapGenerator.data.Clear();

        var tickManager = Current.Game.tickManager;
        int startTick = tickManager.gameStartAbsTick;

        var map = new Map { generationTick = GenTicks.TicksGame };
        GeneratingPreviewMap.Value = map;

        Rand.PushState(request.Seed);

        try
        {
            MapGenerator.mapBeingGenerated = map;

            if (startTick == 0 && Current.ProgramState == ProgramState.Entry)
            {
                tickManager.gameStartAbsTick = GenTicks.ConfiguredTicksAbsAtGameStart;
            }

            var mapParent = new MapParent { Tile = request.MapTile, def = WorldObjectDefOf.Settlement };
            mapParent.SetFaction(Faction.OfPlayer);

            map.info.Size = new IntVec3(request.MapSize.x, 1, request.MapSize.z);
            map.info.parent = mapParent;
            map.ConstructComponents();

            result.Map = map;

            var previewGenStep = new PreviewTextureGenStep(result, request.UseTrueTerrainColors);
            var previewGenStepDef = new GenStepDef { genStep = previewGenStep, order = 9999 };
            var genStepWithParamses = request.GeneratorDef.genSteps
                .Where(s => request.GenStepFilter(s))
                .Select(x => new GenStepWithParams(x, new GenStepParams()))
                .Append(new GenStepWithParams(previewGenStepDef, new GenStepParams()));

            MapGenerator.GenerateContentsIntoMap(genStepWithParamses, map, request.Seed);

            map.weatherManager.EndAllSustainers();
            Find.SoundRoot.sustainerManager.EndAllInMap(map);
            Find.TickManager.RemoveAllFromMap(map);
            Find.Archive.Notify_MapRemoved(map);
        }
        finally
        {
            Rand.PopState();
            MapGenerator.mapBeingGenerated = null;

            if (startTick == 0 && Current.ProgramState == ProgramState.Entry)
            {
                tickManager.gameStartAbsTick = 0;
            }
            
            GeneratingPreviewMap.Value = null;
            MapGenerator.data.Clear();
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

        str.AppendLine("World Seed Hash: " + Find.World?.info?.Seed);
        str.AppendLine("World Tile: " + tileId);
        str.AppendLine("Tick Speed: " + tickManager?.CurTimeSpeed);
        str.AppendLine("Biome: " + tile.biome?.defName);
        str.AppendLine("Biome TPMs: " + tile.biome?.terrainPatchMakers?.Count);
        str.AppendLine("Biome TTresh: " + tile.biome?.terrainsByFertility?.Count);
        str.AppendLine("Biome MCP: " + tile.biome?.modContentPack?.Name);
        if (tile.Rivers != null) str.AppendLine("River: " + tile.Rivers.Count);
        if (tile.Roads != null) str.AppendLine("Road: " + tile.Roads.Count);

        return str.ToString();
    }

    private class PreviewTextureGenStep : GenStep
    {
        private readonly MapPreviewResult _result;
        private readonly bool _useTrueTerrainColors;

        public PreviewTextureGenStep(MapPreviewResult result, bool useTrueTerrainColors)
        {
            _result = result;
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
                    _result.InvalidCells++;
                }
                else if (MapGenerator.Elevation[cell] >= 0.7 && !terrainDef.IsRiver)
                {
                    pixelColor = MapGenerator.Caves[cell] > 0 ? CaveColor : SolidStoneColor;
                }
                else if (!colors.TryGetValue(terrainDef.defName, out pixelColor))
                {
                    pixelColor = MissingTerrainColor;
                }

                _result.SetPixel(cell.x, cell.z, pixelColor);
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
            for (int z = 1; z < result.MapSize.z - 1; z++)
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
