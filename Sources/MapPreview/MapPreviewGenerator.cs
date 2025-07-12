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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LunarFramework.Patching;
using MapPreview.Promises;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

#if !RW_1_6_OR_GREATER
using MapPreview.Patches;
#endif

namespace MapPreview;

/// <summary>
/// Modified version of MapReroll.MapPreviewGenerator that uses full MapGenerator mechanics for better mod compatibility.
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

                    var completedRequest = request;

                    MapPreviewAPI.LunarAPI.LifecycleHooks.DoOnce(() =>
                    {
                        if (_queuedRequests.Count == 0 && CurrentRequest == null)
                        {
                            MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
                        }

                        if (rejectException == null)
                        {
                            completedRequest.Promise.Resolve(result);
                        }
                        else
                        {
                            MapPreviewAPI.Logger.Error("Failed to generate map preview!", rejectException);
                            MapPreviewAPI.Logger.Log("Map Info: \n" + PrintDebugInfo(completedRequest));
                            completedRequest.Promise.Reject(rejectException);
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
        var tickManager = Current.ProgramState != ProgramState.Entry ? Find.TickManager : null;
        var speedWas = tickManager?.CurTimeSpeed ?? TimeSpeed.Paused;

        try
        {
            MapPreviewAPI.IsGeneratingPreview = true;

            #if !RW_1_6_OR_GREATER
            Patch_RimWorld_GenStep_Terrain.SkipRiverFlowCalc = request.SkipRiverFlowCalc;
            #endif

            tickManager?.Pause();

            request.Timer.Start();

            GenerateContentsIntoPreview(request, result);

            AddBevelToSolidStone(result);

            request.Timer.Stop();
        }
        finally
        {
            MapPreviewAPI.IsGeneratingPreview = false;

            #if !RW_1_6_OR_GREATER
            Patch_RimWorld_GenStep_Terrain.SkipRiverFlowCalc = false;
            #endif

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

        #if RW_1_6_OR_GREATER
        MapGenerator.ClearWorkingData();
        #else
        MapGenerator.data.Clear();
        #endif

        var tickManager = Current.Game.tickManager;
        int startTick = tickManager.gameStartAbsTick;

        var map = new Map { generationTick = GenTicks.TicksGame };
        GeneratingPreviewMap.Value = map;

        var previewGenStep = new PreviewTextureGenStep(result, request.UseTrueTerrainColors);
        var previewGenStepDef = new GenStepDef { genStep = previewGenStep, order = 9999 };

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

            #if RW_1_5_OR_GREATER
            map.generatorDef = request.GeneratorDef;
            #endif

            #if RW_1_6_OR_GREATER
            map.events = new MapEvents(map);
            #endif

            map.info.Size = new IntVec3(request.MapSize.x, 1, request.MapSize.z);
            map.info.parent = mapParent;

            if (request.UseMinimalMapComponents)
            {
                ConstructMinimalMapComponents(map);
                Rand.Range(0, 1); // compensate for GasGrid ctor
            }
            else
            {
                map.ConstructComponents();
            }

            result.Map = map;

            #if RW_1_6_OR_GREATER

            foreach (var mutator in map.TileInfo.Mutators)
                mutator.Worker?.Init(map);

            #endif

            var genStepWithParamses = CollectGenStepsForTile(request.GeneratorDef, map.TileInfo)
                .Where(g => request.GenStepFilter(g)).Append(previewGenStepDef)
                .Select(g => new GenStepWithParams(g, new GenStepParams()));

            MapGenerator.GenerateContentsIntoMap(genStepWithParamses, map, request.Seed);

            Find.SoundRoot.sustainerManager.EndAllInMap(map);
            Find.TickManager.RemoveAllFromMap(map);

            #if RW_1_5_OR_GREATER

            map.mapDrawer = null;
            map.Dispose();

            #endif
        }
        finally
        {
            if (startTick == 0 && Current.ProgramState == ProgramState.Entry)
            {
                tickManager.gameStartAbsTick = 0;
            }

            try
            {
                Rand.PopState();
            }
            finally
            {
                MapGenerator.mapBeingGenerated = null;
                GeneratingPreviewMap.Value = null;

                #if RW_1_6_OR_GREATER
                MapGenerator.ClearWorkingData();
                #else
                MapGenerator.data.Clear();
                #endif
            }
        }
    }

    private static IEnumerable<GenStepDef> CollectGenStepsForTile(MapGeneratorDef mapGenerator, Tile tileInfo) =>
        mapGenerator.genSteps.Where(IsAllowedByScenario)
            #if RW_1_6_OR_GREATER
            .Concat(tileInfo.Mutators.SelectMany(m => m.extraGenSteps))
            .Concat(tileInfo.PrimaryBiome.extraGenSteps.Where(IsAllowedByScenario))
            .Except(tileInfo.Mutators.SelectMany(m => m.preventGenSteps))
            .Except(tileInfo.PrimaryBiome.preventGenSteps)
            #endif
            .Distinct();

    private static bool IsAllowedByScenario(GenStepDef g)
    {
        return !Find.Scenario.AllParts.Any(p => typeof (ScenPart_DisableMapGen).IsAssignableFrom(p.def.scenPartClass) && p.def.genStep == g);
    }

    private static string PrintDebugInfo(MapPreviewRequest request)
    {
        var worldGrid = Find.WorldGrid;
        if (worldGrid == null) return "No WorldGrid";

        var tile = worldGrid[request.MapTile];
        if (tile == null) return "No Tile";

        var tickManager = Find.TickManager;

        var str = new StringBuilder();

        str.AppendLine("World Seed Hash: " + Find.World?.info?.Seed);
        str.AppendLine("World Tile: " + request.MapTile);

        str.AppendLine("Biome: " + tile.biome?.defName);
        str.AppendLine("Biome TPMs: " + tile.biome?.terrainPatchMakers?.Count);
        str.AppendLine("Biome TTresh: " + tile.biome?.terrainsByFertility?.Count);
        str.AppendLine("Biome MCP: " + tile.biome?.modContentPack?.Name);

        str.AppendLine("Tick Speed: " + tickManager?.CurTimeSpeed);

        str.AppendLine("Map Seed: " + request.Seed);
        str.AppendLine("Map Size: " + request.MapSize.x + "x" + request.MapSize.z);
        str.AppendLine("Map Comps: " + (request.UseMinimalMapComponents ? "Minimal" : "Full"));
        str.AppendLine("Color Palette: " + (request.UseTrueTerrainColors ? "Dynamic" : "Fixed"));

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

    internal static readonly HashSet<string> IncludedMapComponentsMinimal = new()
    {
        // Vanilla
        typeof(RoadInfo).FullName,
        typeof(WaterInfo).FullName,

        #if RW_1_6_OR_GREATER
        typeof(MixedBiomeMapComponent).FullName,
        #endif

        // Geological Landforms
        "GeologicalLandforms.BiomeGrid",

        // Advanced Biomes and other old mods
        "ActiveTerrain.SpecialTerrainList",

        #if RW_1_6_OR_GREATER

        // Dubs Bad Hygiene
        "DubsBadHygiene.MapComponent_Hygiene",

        #endif
    };

    internal static readonly HashSet<string> IncludedMapComponentsFull = new(IncludedMapComponentsMinimal)
    {
        #if !RW_1_6_OR_GREATER

        // Dubs Bad Hygiene
        "DubsBadHygiene.MapComponent_Hygiene",

        #endif
    };

    private static void ConstructMinimalMapComponents(Map map)
    {
        // ##### Essential #####

        map.cellIndices = new CellIndices(map);                                                             // req
        map.cellsInRandomOrder = new MapCellsInRandomOrder(map);                                            // req
        map.floodFiller = new FloodFiller(map);                                                             // req

        // ##### Main grids #####

        map.terrainGrid = new TerrainGrid(map);                                                             // req
        map.fertilityGrid = new FertilityGrid(map);                                                         // req
        map.pollutionGrid = new PollutionGrid(map);                                                         // req
        map.edificeGrid = new EdificeGrid(map);                                                             // req
        map.roofGrid = new RoofGrid(map);                                                                   // req

        // ##### Special grids #####

        map.fogGrid = new FogGrid(map);                                                                     // light
        map.glowGrid = new GlowGrid(map);                                                                   // trial
        // map.glowFlooder = new GlowFlooder(map);                                                          // safe
        // map.deepResourceGrid = new DeepResourceGrid(map);                                                // trial
        // map.snowGrid = new SnowGrid(map);                                                                // trial
        // map.gasGrid = new GasGrid(map);                                                                  // trial

        // ##### Regions and rooms #####

        map.regionGrid = new RegionGrid(map);                                                               // likely
        map.regionAndRoomUpdater = new RegionAndRoomUpdater(map);                                           // likely
        // map.regionMaker = new RegionMaker(map);                                                          // safe
        // map.regionLinkDatabase = new RegionLinkDatabase();                                               // safe
        // map.autoBuildRoofAreaSetter = new AutoBuildRoofAreaSetter(map);                                  // safe
        map.regionDirtyer = new RegionDirtyer(map);                                                         // likely
        map.roofCollapseBuffer = new RoofCollapseBuffer();                                                  // light
        map.roofCollapseBufferResolver = new RoofCollapseBufferResolver(map);                               // light

        // ##### Things #####

        map.thingGrid = new ThingGrid(map);                                                                 // likely
        // map.coverGrid = new CoverGrid(map);                                                              // trial
        // map.linkGrid = new LinkGrid(map);                                                                // trial
        // map.blueprintGrid = new BlueprintGrid(map);                                                      // trial
        map.spawnedThings = new ThingOwner<Thing>(map);                                                     // likely
        map.listerThings = new ListerThings(ListerThingsUse.Global);                                        // likely
        map.listerBuildings = new ListerBuildings();                                                        // likely
        // map.haulDestinationManager = new HaulDestinationManager(map);                                    // trial
        // map.gatherSpotLister = new GatherSpotLister();                                                   // trial
        // map.listerBuildingsRepairable = new ListerBuildingsRepairable();                                 // trial
        // map.listerHaulables = new ListerHaulables(map);                                                  // trial
        // map.listerMergeables = new ListerMergeables(map);                                                // trial
        // map.listerFilthInHomeArea = new ListerFilthInHomeArea(map);                                      // trial
        // map.listerArtificialBuildingsForMeditation = new ListerArtificialBuildingsForMeditation(map);    // trial
        // map.listerBuldingOfDefInProximity = new ListerBuldingOfDefInProximity(map);                      // trial
        // map.listerBuildingWithTagInProximity = new ListerBuildingWithTagInProximity(map);                // trial
        map.damageWatcher = new DamageWatcher();                                                            // light
        map.wealthWatcher = new WealthWatcher(map);                                                         // light
        // map.resourceCounter = new ResourceCounter(map);                                                  // trial
        // map.treeDestructionTracker = new TreeDestructionTracker(map);                                    // trial
        // map.animalPenManager = new AnimalPenManager(map);                                                // trial
        // map.plantGrowthRateCalculator = new MapPlantGrowthRateCalculator();                              // trial
        // map.powerNetGrid = new PowerNetGrid(map);                                                        // trial
        // map.powerNetManager = new PowerNetManager(map);                                                  // trial
        map.moteCounter = new MoteCounter();                                                                // light

        // ##### Pawns #####

        map.mapPawns = new MapPawns(map);                                                                   // likely
        map.lordManager = new LordManager(map);                                                             // light
        // map.attackTargetsCache = new AttackTargetsCache(map);                                            // trial
        // map.attackTargetReservationManager = new AttackTargetReservationManager(map);                    // safe
        // map.pawnDestinationReservationManager = new PawnDestinationReservationManager();                 // safe
        // map.reservationManager = new ReservationManager(map);                                            // trial
        // map.enrouteManager = new EnrouteManager(map);                                                    // safe
        // map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();         // safe
        // map.lordsStarter = new VoluntarilyJoinableLordsStarter(map);                                     // safe
        // map.pawnPathPool = new PawnPathPool(map);                                                        // safe
        // map.itemAvailability = new ItemAvailability(map);                                                // trial
        // map.strengthWatcher = new StrengthWatcher(map);                                                  // safe
        // map.mineStrikeManager = new MineStrikeManager();                                                 // safe
        // map.autoSlaughterManager = new AutoSlaughterManager(map);                                        // safe
        // map.deferredSpawner = new DeferredSpawner(map);                                                  // safe

        // ##### Environment #####

        map.storyState = new StoryState(map);                                                               // light
        map.dangerWatcher = new DangerWatcher(map);                                                         // light
        map.retainedCaravanData = new RetainedCaravanData(map);                                             // light
        map.gameConditionManager = new GameConditionManager(map);                                           // light
        map.weatherManager = new WeatherManager(map);                                                       // light
        map.mapTemperature = new MapTemperature(map);                                                       // light
        // map.temperatureCache = new TemperatureCache(map);                                                // safe
        map.windManager = new WindManager(map);                                                             // light
        map.steadyEnvironmentEffects = new SteadyEnvironmentEffects(map);                                   // light
        map.skyManager = new SkyManager(map);                                                               // light
        map.weatherDecider = new WeatherDecider(map);                                                       // light
        map.fireWatcher = new FireWatcher(map);                                                             // light
        map.passingShipManager = new PassingShipManager(map);                                               // light
        map.wildAnimalSpawner = new WildAnimalSpawner(map);                                                 // light
        map.wildPlantSpawner = new WildPlantSpawner(map);                                                   // light

        // ##### Rendering and UI #####

        map.mapDrawer = new MapDrawer(map);                                                                 // light
        // map.dynamicDrawManager = new DynamicDrawManager(map);                                            // safe
        // map.tooltipGiverList = new TooltipGiverList();                                                   // safe
        map.debugDrawer = new DebugCellDrawer();                                                            // light
        // map.overlayDrawer = new OverlayDrawer();                                                         // safe
        // map.rememberedCameraPos = new RememberedCameraPos(map);                                          // safe
        // map.temporaryThingDrawer = new TemporaryThingDrawer();                                           // safe
        // map.postTickVisuals = new PostTickVisuals(map);                                                  // safe
        // map.effecterMaintainer = new EffecterMaintainer(map);                                            // trial
        // map.flecks = new FleckManager(map);                                                              // trial

        // ##### Pathing #####

        map.pathing = new Pathing(map);                                                                     // req
        map.exitMapGrid = new ExitMapGrid(map);                                                             // light
        // map.avoidGrid = new AvoidGrid(map);                                                              // trial
        // map.pathFinder = new PathFinder(map);                                                            // trial
        // map.reachability = new Reachability(map);                                                        // trial

        // ##### Player Designations #####

        // map.designationManager = new DesignationManager(map);                                            // trial
        // map.zoneManager = new ZoneManager(map);                                                          // trial
        map.areaManager = new AreaManager(map);                                                             // light
        map.storageGroups = new StorageGroupManager(map);                                                   // light

        // ##### Custom Components #####

        map.components.Clear();

        foreach (var type in typeof(MapComponent).AllSubclassesNonAbstract())
        {
            if (IncludedMapComponentsMinimal.Contains(type.FullName))
            {
                try
                {
                    map.components.Add((MapComponent) Activator.CreateInstance(type, map));
                }
                catch (Exception ex)
                {
                    Log.Error("Could not instantiate a MapComponent of type " + type + ": " + ex);
                }
            }
        }

        map.roadInfo = map.GetComponent<RoadInfo>();
        map.waterInfo = map.GetComponent<WaterInfo>();
    }
}
