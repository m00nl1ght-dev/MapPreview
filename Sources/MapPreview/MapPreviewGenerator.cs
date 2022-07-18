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
using MapPreview.Patches;
using MapPreview.Promises;
using MapPreview.Util;
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
    
    private static readonly ThreadLocal<Map> GeneratingPreviewMap = new();
    
    private readonly Queue<QueuedPreviewRequest> _queuedRequests = new();
    private Thread _workerThread;
    private EventWaitHandle _workHandle = new AutoResetEvent(false);
    private EventWaitHandle _disposeHandle = new AutoResetEvent(false);
    private bool _disposed;

    private static FieldInfo _fieldMapGenData;

    private static readonly Color MissingTerrainColor = new(0.38f, 0.38f, 0.38f);
    private static readonly Color SolidStoneColor = GenColor.FromHex("36271C");
    private static readonly Color SolidStoneHighlightColor = GenColor.FromHex("4C3426");
    private static readonly Color SolidStoneShadowColor = GenColor.FromHex("1C130E");
    private static readonly Color CaveColor = GenColor.FromHex("42372b");

    private static string debugPre, debugPost;

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

    public IPromise<ThreadableTexture> QueuePreviewForSeed(
        string seed, int mapTile, int mapSize, 
        int targetTextureSize, 
        bool useTrueTerrainColors, 
        Color[] existingBuffer = null)
    {
        if (_disposeHandle == null)
        {
            throw new Exception("ExactMapPreviewGenerator has already been disposed.");
        }
        
        if (mapSize > targetTextureSize)
        {
            throw new Exception("Map size exceeds max preview size: " + mapSize + " > " + targetTextureSize);
        }

        if (_workerThread == null)
        {
            _fieldMapGenData ??= AccessTools.Field(typeof(MapGenerator), "data");
            if (_fieldMapGenData == null) throw new Exception("Failed to reflect MapGenerator.data");
            
            _workerThread = new Thread(DoThreadWork);
            _workerThread.Start();
        }
        
        Log.ResetMessageCount();

        var promise = new Promise<ThreadableTexture>();
        _queuedRequests.Enqueue(new QueuedPreviewRequest(promise, targetTextureSize, useTrueTerrainColors, seed, mapTile, mapSize, existingBuffer));
        _workHandle.Set();
        
        return promise;
    }

    private void DoThreadWork()
    {
        QueuedPreviewRequest request = null;
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

                    ThreadableTexture placeholderTex = null;
                    try
                    {
                        placeholderTex = new ThreadableTexture(request.MapTile, request.MapSize, request.TargetTextureSize, request.ExistingBuffer);
                        GeneratePreviewForSeed(request.Seed, request.MapTile, request.MapSize, placeholderTex, request.UseTrueTerrainColors);

                        if (placeholderTex.MapGenErrored)
                        {
                            throw new Exception("No terrain was generated for at least one map cell.");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ResetMessageCount();
                        Log.Error("Failed to generate map preview: " + e);
                        debugPost = PrintMapTileInfo(request.MapTile);
                        Log.Message("Map Info at start: \n" + debugPre);
                        Log.Message("Map Info at error: \n" + debugPost);
                        rejectException = e;
                    }

                    var promise = request.Promise;
                    LifecycleHooks.OnUpdateOnce += () =>
                    {
                        if (rejectException == null)
                        {
                            promise.Resolve(placeholderTex);
                        }
                        else
                        {
                            promise.Reject(rejectException);
                        }
                    };
                }
            }

            _workHandle.Close();
            _disposeHandle.Close();
            _disposeHandle = _workHandle = null;
        }
        catch (Exception e)
        {
            LifecycleHooks.OnUpdateOnce += () =>
            {
                Log.ResetMessageCount();
                Log.Error("Exception in preview generator thread: " + e);
                request?.Promise.Reject(e);
            };
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
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
        if (!_disposed || !_workerThread.IsAlive || _workerThread.ThreadState == ThreadState.WaitSleepJoin) return;
        _workerThread.Join(5 * 1000);
    }

    private static void GeneratePreviewForSeed(
        string seed, 
        int mapTile, 
        int mapSize, 
        ThreadableTexture texture, 
        bool useTrueTerrainColors)
    {
        var tickManager = Find.TickManager;
        var speedWas = tickManager?.CurTimeSpeed ?? TimeSpeed.Paused;
        var prevSeed = Find.World.info.seedString;
        debugPre = PrintMapTileInfo(mapTile);

        try
        {
            Main.IsGeneratingPreview = true;
            Find.World.info.seedString = seed;
            RimWorld_TerrainPatchMaker.Reset();
            
            tickManager?.Pause();

            var mapParent = new MapParent { Tile = mapTile, def = WorldObjectDefOf.Settlement};
            mapParent.SetFaction(Faction.OfPlayer);

            var mapSizeVec = new IntVec3(mapSize, 1, mapSize);
            GenerateMap(mapSizeVec, mapParent, MapGeneratorDefOf.Base_Player, texture, useTrueTerrainColors);

            AddBevelToSolidStone(texture, mapSize);
        }
        catch (Exception e)
        {
            Log.ResetMessageCount();
            Log.Error("Error in preview generation: " + e);
            Debug.LogException(e);
            texture.MapGenErrored = true;
            debugPost = PrintMapTileInfo(mapTile);
            Log.Message("Map Info at start: \n" + debugPre);
            Log.Message("Map Info at error: \n" + debugPost);
        }
        finally
        {
            RimWorld_TerrainPatchMaker.Reset();
            Find.World.info.seedString = prevSeed;
            Main.IsGeneratingPreview = false;
            
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
        ThreadableTexture texture,
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

        var str = new StringBuilder();

        str.AppendLine("World Seed: " + Find.World?.info?.seedString);
        str.AppendLine("World Tile: " + tileId);
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
        private readonly ThreadableTexture _texture;
        private readonly bool _useTrueTerrainColors;

        public PreviewTextureGenStep(ThreadableTexture texture, bool useTrueTerrainColors)
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
    private static void AddBevelToSolidStone(ThreadableTexture tex, int mapSize)
    {
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                var isStone = tex.GetPixel(x, y) == SolidStoneColor;
                if (isStone)
                {
                    var colorBelow = y > 0 ? tex.GetPixel(x, y - 1) : Color.clear;
                    var isStoneBelow = colorBelow == SolidStoneColor || colorBelow == SolidStoneHighlightColor ||
                                       colorBelow == SolidStoneShadowColor;
                    var isStoneAbove = y < mapSize - 1 && tex.GetPixel(x, y + 1) == SolidStoneColor;
                    if (!isStoneAbove)
                    {
                        tex.SetPixel(x, y, SolidStoneHighlightColor);
                    }
                    else if (!isStoneBelow)
                    {
                        tex.SetPixel(x, y, SolidStoneShadowColor);
                    }
                }
            }
        }
    }

    private class QueuedPreviewRequest
    {
        public readonly Promise<ThreadableTexture> Promise;
        public readonly int TargetTextureSize;
        public readonly bool UseTrueTerrainColors;
        public readonly Color[] ExistingBuffer;
        
        public readonly string Seed;
        public readonly int MapTile;
        public readonly int MapSize;

        public QueuedPreviewRequest(
            Promise<ThreadableTexture> promise, 
            int targetTextureSize,
            bool useTrueTerrainColors, 
            string seed, int mapTile, int mapSize,
            Color[] existingBuffer = null)
        {
            Promise = promise;
            TargetTextureSize = targetTextureSize;
            UseTrueTerrainColors = useTrueTerrainColors;
            ExistingBuffer = existingBuffer;
            Seed = seed;
            MapTile = mapTile;
            MapSize = mapSize;
        }
    }

    // A placeholder for Texture2D that can be used in threads other than the main one (required since 1.0)
    // Pixels are laid out left to right, top to bottom
    public class ThreadableTexture
    {
        public readonly Color[] Pixels;
        
        public readonly int TextureSize;
        public readonly int MapSize;
        public readonly int MapTile;
        
        public Rect TexCoords => new(0, 0, MapSize / (float) TextureSize, MapSize / (float) TextureSize);

        public bool MapGenErrored;

        public ThreadableTexture(int mapTile, int mapSize, int textureSize, Color[] existingBuffer = null)
        {
            MapTile = mapTile;
            MapSize = mapSize;
            TextureSize = textureSize;
            if (existingBuffer != null && existingBuffer.Length == textureSize * textureSize) Pixels = existingBuffer;
            else Pixels = new Color[textureSize * textureSize];
        }

        public void SetPixel(int x, int y, Color color)
        {
            Pixels[y * TextureSize + x] = color;
        }

        public Color GetPixel(int x, int y)
        {
            return Pixels[y * TextureSize + x];
        }

        public void CopyToTexture(Texture2D tex)
        {
            tex.SetPixels(Pixels);
        }
    }
}