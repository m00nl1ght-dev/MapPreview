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
using System.Threading;
using HarmonyLib;
using HugsLib;
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
public class ExactMapPreviewGenerator : IDisposable
{
    public static Map GeneratingMapOnCurrentThread => Main.IsGeneratingPreview ? GeneratingPreviewMap.Value : null;
    public static bool IsGeneratingOnCurrentThread => Main.IsGeneratingPreview && GeneratingPreviewMap.Value != null;

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

    public IPromise<ThreadableTexture> QueuePreviewForSeed(string seed, int mapTile, int mapSize, int targetTextureSize, Color[] existingBuffer = null)
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

        var promise = new Promise<ThreadableTexture>();
        _queuedRequests.Enqueue(new QueuedPreviewRequest(promise, targetTextureSize, seed, mapTile, mapSize, existingBuffer));
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
                        GeneratePreviewForSeed(request.Seed, request.MapTile, request.MapSize, placeholderTex);

                        if (placeholderTex.MapGenErrored)
                        {
                            throw new Exception("No terrain was generated for at least one map cell.");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("Failed to generate map preview: " + e);
                        rejectException = e;
                    }

                    var promise = request.Promise;
                    HugsLibController.Instance.DoLater.DoNextUpdate(() =>
                    {
                        if (rejectException == null)
                        {
                            promise.Resolve(placeholderTex);
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
            HugsLibController.Instance.DoLater.DoNextUpdate(() =>
            {
                Log.Error("Exception in preview generator thread: " + e);
                request?.Promise.Reject(e);
            });
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

    private static void GeneratePreviewForSeed(string seed, int mapTile, int mapSize, ThreadableTexture texture)
    {
        var prevSeed = Find.World.info.seedString;

        try
        {
            Main.IsGeneratingPreview = true;
            Find.World.info.seedString = seed;
            RimWorld_TerrainPatchMaker.Reset();

            var mapParent = new MapParent { Tile = mapTile, def = WorldObjectDefOf.Settlement};
            mapParent.SetFaction(Faction.OfPlayer);
            
            GenerateMap(new IntVec3(mapSize, 1, mapSize), mapParent, MapGeneratorDefOf.Base_Player, texture);

            AddBevelToSolidStone(texture, mapSize);
        }
        catch (Exception e)
        {
            Log.Error("Error in preview generation: " + e);
            Debug.LogException(e);
            texture.MapGenErrored = true;
        }
        finally
        {
            RimWorld_TerrainPatchMaker.Reset();
            Find.World.info.seedString = prevSeed;
            Main.IsGeneratingPreview = false;
        }
    }

    private static void GenerateMap(
        IntVec3 mapSize,
        MapParent parent,
        MapGeneratorDef mapGenerator,
        ThreadableTexture texture)
    {
        var mapGeneratorData = (Dictionary<string, object>) _fieldMapGenData.GetValue(null);
        mapGeneratorData.Clear();
        
        Rand.PushState();
        int seed = Gen.HashCombineInt(Find.World.info.Seed, parent.Tile);
        Rand.Seed = seed;

        try
        {
            var map = new Map { generationTick = GenTicks.TicksGame };
            MapGenerator.mapBeingGenerated = map;
            GeneratingPreviewMap.Value = map;
            map.info.Size = mapSize;
            map.info.parent = parent;
            map.ConstructComponents();

            var previewGenStep = new GenStepDef { genStep = new PreviewTextureGenStep(texture), order = 9999 };
            var genStepWithParamses = mapGenerator.genSteps
                .Where(d => IncludedGenStepDefs.Contains(d.defName))
                .Select(x => new GenStepWithParams(x, new GenStepParams()))
                .Append(new GenStepWithParams(previewGenStep, new GenStepParams()));
                
            MapGenerator.GenerateContentsIntoMap(genStepWithParamses, map, seed);
            
            map.weatherManager.EndAllSustainers();
            Find.SoundRoot.sustainerManager.EndAllInMap(map);
            Find.TickManager.RemoveAllFromMap(map);
            Find.Archive.Notify_MapRemoved(map);
        }
        finally
        {
            MapGenerator.mapBeingGenerated = null;
            GeneratingPreviewMap.Value = null;
            Rand.PopState();
        }
    }

    private class PreviewTextureGenStep : GenStep
    {
        private readonly ThreadableTexture _texture;

        public PreviewTextureGenStep(ThreadableTexture texture)
        {
            _texture = texture;
        }

        public override int SeedPart => 0;

        public override void Generate(Map map, GenStepParams parms)
        {
            var mapBounds = CellRect.WholeMap(map);
            var colors = TrueTerrainColors.CurrentTerrainColors;
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
        public Color[] ExistingBuffer;
        
        public readonly string Seed;
        public readonly int MapTile;
        public readonly int MapSize;

        public QueuedPreviewRequest(Promise<ThreadableTexture> promise, int targetTextureSize, string seed, int mapTile, int mapSize, Color[] existingBuffer = null)
        {
            Promise = promise;
            TargetTextureSize = targetTextureSize;
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