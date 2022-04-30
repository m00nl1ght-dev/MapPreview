using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using HugsLib;
using MapPreview;
using MapReroll.Promises;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapReroll {
	
	/// <summary>
	/// Modified version of MapReroll.MapPreviewGenerator that uses original TerrainAt method for better mod compat.
	/// </summary>
	public class ExactMapPreviewGenerator : IDisposable {

		private readonly Queue<QueuedPreviewRequest> queuedRequests = new();
		private Thread workerThread;
		private EventWaitHandle workHandle = new AutoResetEvent(false);
		private EventWaitHandle disposeHandle = new AutoResetEvent(false);
		private EventWaitHandle mainThreadHandle = new AutoResetEvent(false);
		private bool disposed;
		
		private static readonly Color missingTerrainColor = new Color(0.38f, 0.38f, 0.38f);
		private static readonly Color solidStoneColor = GenColor.FromHex("36271C");
		private static readonly Color solidStoneHighlightColor = GenColor.FromHex("4C3426");
		private static readonly Color solidStoneShadowColor = GenColor.FromHex("1C130E");
		private static readonly Color caveColor = GenColor.FromHex("42372b");
		
		public static MethodInfo GenStepTerrain_TerrainAt { get; private set; }

		public static void InitReflection()
		{
			GenStepTerrain_TerrainAt = AccessTools.Method(typeof(GenStep_Terrain), "TerrainFrom");
		}

		public IPromise<Texture2D> QueuePreviewForSeed(string seed, int mapTile, int mapSize, bool revealCaves) {
			if (disposeHandle == null) {
				throw new Exception("MapPreviewGenerator has already been disposed.");
			}
			var promise = new Promise<Texture2D>();
			if (workerThread == null) {
				workerThread = new Thread(DoThreadWork);
				workerThread.Start();
			}
			queuedRequests.Enqueue(new QueuedPreviewRequest(promise, seed, mapTile, mapSize, revealCaves));
			workHandle.Set();
			return promise;
		}

		private void DoThreadWork() {
			QueuedPreviewRequest request = null;
			try {
				while (queuedRequests.Count > 0 || WaitHandle.WaitAny(new WaitHandle[] {workHandle, disposeHandle}) == 0) {
					Exception rejectException = null;
					if (queuedRequests.Count > 0) {
						var req = queuedRequests.Dequeue();
						request = req;
						Texture2D texture = null;
						int width = 0, height = 0;
						WaitForExecutionInMainThread(() => {
							// textures must be instantiated in the main thread
							texture = new Texture2D(req.MapSize, req.MapSize, TextureFormat.RGB24, false);
							width = texture.width;
							height = texture.height;
						});
						ThreadableTexture placeholderTex = null;
						try {
							if (texture == null) {
								throw new Exception("Could not create required texture.");
							}
							placeholderTex = new ThreadableTexture(width, height);
							GeneratePreviewForSeed(req.Seed, req.MapTile, req.MapSize, req.RevealCaves, placeholderTex);
						} catch (Exception e) {
							Log.Error("Failed to generate map preview: " + e);
							rejectException = e;
							texture = null;
						}
						if (texture != null && placeholderTex != null) {
							WaitForExecutionInMainThread(() => {
								// upload in main thread
								placeholderTex.CopyToTexture(texture);
								texture.Apply();
							});
						}
						WaitForExecutionInMainThread(() => {
							if (texture == null) {
								req.Promise.Reject(rejectException);
							} else {
								req.Promise.Resolve(texture);
							}
						});
					}
				}
				workHandle.Close();
				mainThreadHandle.Close();
				disposeHandle.Close();
				mainThreadHandle = disposeHandle = workHandle = null;
			} catch (Exception e) {
				Log.Error("Exception in preview generator thread: " + e);
				if (request != null) {
					request.Promise.Reject(e);
				}
			}
		}

		public void Dispose() {
			if (disposed) {
				throw new Exception("MapPreviewGenerator has already been disposed.");
			}
			disposed = true;
			queuedRequests.Clear();
			disposeHandle.Set();
		}

		/// <summary>
		/// The worker cannot be aborted- wait for the worker to complete before generating map
		/// </summary>
		public void WaitForDisposal() {
			if (!disposed || !workerThread.IsAlive || workerThread.ThreadState == ThreadState.WaitSleepJoin) return;
			LongEventHandler.QueueLongEvent(() => workerThread.Join(60 * 1000), "Reroll2_finishingPreview", true, null);
		}

		/// <summary>
		/// Block until delegate is executed or times out
		/// </summary>
		private void WaitForExecutionInMainThread(Action action) {
			if (mainThreadHandle == null) return;
			HugsLibController.Instance.DoLater.DoNextUpdate(() => {
				action();
				mainThreadHandle.Set();
			});
			mainThreadHandle.WaitOne(1000);
		}

		private static void GeneratePreviewForSeed(string seed, int mapTile, int mapSize, bool revealCaves, ThreadableTexture texture) {
			var prevSeed = Find.World.info.seedString;

			try {
				MapRerollController.HasCavesOverride.HasCaves = Find.World.HasCaves(mapTile);
				MapRerollController.HasCavesOverride.OverrideEnabled = true;
				Find.World.info.seedString = seed;

				MapRerollController.RandStateStackCheckingPaused = true;
				var grids = GenerateMapGrids(mapTile, mapSize, revealCaves);
				DeepProfiler.Start("generateMapPreviewTexture");
				const string terrainGenStepName = "Terrain";
				var terrainGenStepDef = DefDatabase<GenStepDef>.GetNamedSilentFail(terrainGenStepName);
				if (terrainGenStepDef == null) throw new Exception("Named GenStepDef not found: " + terrainGenStepName);
				var terrainGenstep = terrainGenStepDef.genStep;
				var riverMaker = ReflectionCache.GenStepTerrain_GenerateRiver.Invoke(terrainGenstep, new object[] {grids.Map});
				BeachMaker.Init(grids.Map);

				var mapBounds = CellRect.WholeMap(grids.Map);
				foreach (var cell in mapBounds) {
					const float rockCutoff = .7f;
					var terrainDef = (TerrainDef) GenStepTerrain_TerrainAt.Invoke(terrainGenstep, new []{cell, grids.Map, grids.ElevationGrid[cell], grids.FertilityGrid[cell], riverMaker, false});
					if (!TrueTerrainColors.TrueTerrainColorsReadonly.TryGetValue(terrainDef.defName, out Color pixelColor)) {
						pixelColor = missingTerrainColor;
					}
					if (grids.ElevationGrid[cell] > rockCutoff && !terrainDef.IsRiver) {
						pixelColor = solidStoneColor;
						if (grids.CavesGrid[cell] > 0) {
							pixelColor = caveColor;
						}
					}
					texture.SetPixel(cell.x, cell.z, pixelColor);
				}

				AddBevelToSolidStone(texture);

				foreach (var terrainPatchMaker in grids.Map.Biome.terrainPatchMakers) {
					terrainPatchMaker.Cleanup();
				}
			} catch (Exception e) {
				Log.Error("Error in preview generation: " + e);
				Debug.LogException(e);
			} finally {
				RockNoises.Reset();
				DeepProfiler.End();
				Find.World.info.seedString = prevSeed;
				MapRerollController.RandStateStackCheckingPaused = false;
				MapRerollController.HasCavesOverride.OverrideEnabled = false;
				try {
					ReflectionCache.BeachMaker_Cleanup.Invoke(null, null);
				} catch (Exception e) {
					Log.Error("Error in BeachMaker preview cleanup: " + e);
					Debug.LogException(e);
				}
			}
		}

		/// <summary>
		/// Adds highlights and shadows to the solid stone color in the texture
		/// </summary>
		private static void AddBevelToSolidStone(ThreadableTexture tex) {
			for (int x = 0; x < tex.width; x++) {
				for (int y = 0; y < tex.height; y++) {
					var isStone = tex.GetPixel(x, y) == solidStoneColor;
					if (isStone) {
						var colorBelow = y > 0 ? tex.GetPixel(x, y - 1) : Color.clear;
						var isStoneBelow = colorBelow == solidStoneColor || colorBelow == solidStoneHighlightColor || colorBelow == solidStoneShadowColor;
						var isStoneAbove = y < tex.height - 1 && tex.GetPixel(x, y + 1) == solidStoneColor;
						if (!isStoneAbove) {
							tex.SetPixel(x, y, solidStoneHighlightColor);
						} else if (!isStoneBelow) {
							tex.SetPixel(x, y, solidStoneShadowColor);
						}
					}
				}
			}
		}

		/// <summary>
		/// Generate a minimal map with elevation and fertility grids
		/// </summary>
		private static MapGridSet GenerateMapGrids(int mapTile, int mapSize, bool revealCaves) {
			DeepProfiler.Start("generateMapPreviewGrids");
			try {
				Rand.PushState();
				var mapGeneratorData = (Dictionary<string, object>)ReflectionCache.MapGenerator_Data.GetValue(null);
				mapGeneratorData.Clear();

				var map = CreateMapStub(mapSize, mapTile);
				MapGenerator.mapBeingGenerated = map;
				
				var mapSeed = Gen.HashCombineInt(Find.World.info.Seed, map.Tile);
				Rand.Seed = mapSeed;
				RockNoises.Init(map);

				var elevationFertilityGenstep = new GenStep_ElevationFertility();
				Rand.Seed = Gen.HashCombineInt(mapSeed, elevationFertilityGenstep.SeedPart);
				elevationFertilityGenstep.Generate(map, new GenStepParams());

				if (revealCaves) {
					var cavesGenstep = new GenStep_Caves();
					Rand.Seed = Gen.HashCombineInt(mapSeed, cavesGenstep.SeedPart);
					cavesGenstep.Generate(map, new GenStepParams());
				}

				var result = new MapGridSet(MapGenerator.Elevation, MapGenerator.Fertility, MapGenerator.Caves, map);
				mapGeneratorData.Clear();

				return result;
			} finally {
				DeepProfiler.End();
				MapGenerator.mapBeingGenerated = null;
				Rand.PopState();
			}
		}

		/// <summary>
		/// Make an absolute bare minimum map instance for grid generation.
		/// </summary>
		private static Map CreateMapStub(int mapSize, int mapTile) {
			var parent = new MapParent {Tile = mapTile};
			var map = new Map {
				info = {
					parent = parent,
					Size = new IntVec3(mapSize, 1, mapSize)
				}
			};
			map.cellIndices = new CellIndices(map);
			map.floodFiller = new FloodFiller(map);
			map.waterInfo = new WaterInfo(map);
			return map;
		}

		private class MapGridSet {
			public readonly MapGenFloatGrid ElevationGrid;
			public readonly MapGenFloatGrid FertilityGrid;
			public readonly MapGenFloatGrid CavesGrid;
			public readonly Map Map;

			public MapGridSet(MapGenFloatGrid elevationGrid, MapGenFloatGrid fertilityGrid, MapGenFloatGrid cavesGrid, Map map) {
				ElevationGrid = elevationGrid;
				FertilityGrid = fertilityGrid;
				CavesGrid = cavesGrid;
				Map = map;
			}
		}

		private class QueuedPreviewRequest {
			public readonly Promise<Texture2D> Promise;
			public readonly string Seed;
			public readonly int MapTile;
			public readonly int MapSize;
			public readonly bool RevealCaves;

			public QueuedPreviewRequest(Promise<Texture2D> promise, string seed, int mapTile, int mapSize, bool revealCaves) {
				Promise = promise;
				Seed = seed;
				MapTile = mapTile;
				MapSize = mapSize;
				RevealCaves = revealCaves;
			}
		}

		// A placeholder for Texture2D that can be used in threads other than the main one (required since 1.0)
		private class ThreadableTexture {
			// pixels are laid out left to right, top to bottom
			private readonly Color[] pixels;
			public readonly int width;
			public readonly int height;

			public ThreadableTexture(int width, int height) {
				this.width = width;
				this.height = height;
				pixels = new Color[width * height];
			}

			public void SetPixel(int x, int y, Color color) {
				pixels[y * height + x] = color;
			}

			public Color GetPixel(int x, int y) {
				return pixels[y * height + x];
			}

			public void CopyToTexture(Texture2D tex) {
				tex.SetPixels(pixels);
			}
		}
	}
}