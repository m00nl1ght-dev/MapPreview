using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldInterface))]
internal class Patch_RimWorld_WorldInterface
{
    internal static int TileId = -1;

    private static bool _activeSinceEnteringMap;
    private static bool _openedPreviewSinceEnteringMap;

    static Patch_RimWorld_WorldInterface() => MapPreviewAPI.OnWorldChanged += Refresh;

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(Patch_RimWorld_WorldInterface));

    [HarmonyPostfix]
    [HarmonyPatch("WorldInterfaceUpdate")]
    private static void WorldInterfaceUpdate(WorldSelector ___selector)
    {
        if (!WorldRendererUtility.WorldRenderedNow)
        {
            if (_activeSinceEnteringMap)
            {
                MapPreviewWindow.Instance?.Close();
                MapPreviewToolbar.Instance?.Close();

                if (_openedPreviewSinceEnteringMap)
                {
                    MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
                    _openedPreviewSinceEnteringMap = false;
                }

                _activeSinceEnteringMap = false;
                TileId = -1;
            }

            return;
        }

        var selectedTileNow = ___selector.selectedTile;
        if (selectedTileNow < 0 && ___selector.NumSelectedObjects == 1)
        {
            var selectedObject = ___selector.SelectedObjects[0];
            if (selectedObject is MapParent)
            {
                selectedTileNow = selectedObject.Tile;
            }
        }

        if (TileId != selectedTileNow)
        {
            var wasAutoSelect = Current.ProgramState == ProgramState.Playing && TileId == -1 && !_activeSinceEnteringMap;

            TileId = selectedTileNow;

            if (TileId >= 0)
            {
                var world = Find.World;
                var worldTile = world.grid[TileId];
                var mapParent = world.worldObjects.MapParentAt(TileId);
                
                _activeSinceEnteringMap = true;

                if (MapPreviewMod.Settings.EnableToolbar)
                {
                    var toolbar = MapPreviewToolbar.Instance;
                    if (toolbar == null) Find.WindowStack.Add(toolbar = new MapPreviewToolbar());
                    toolbar.OnWorldTileSelected(world, TileId, mapParent);
                }

                if (ShouldPreviewForTile(worldTile, TileId, mapParent) && (!wasAutoSelect || MapPreviewMod.Settings.AutoOpenPreviewOnWorldMap))
                {
                    if (MapPreviewMod.Settings.EnableMapPreview && MapPreviewAPI.IsReady)
                    {
                        if (!_openedPreviewSinceEnteringMap)
                        {
                            MapPreviewAPI.SubscribeGenPatches(PatchGroupSubscriber);
                            _openedPreviewSinceEnteringMap = true;
                        }

                        var window = MapPreviewWindow.Instance;
                        if (window == null) Find.WindowStack.Add(window = new MapPreviewWindow());
                        window.OnWorldTileSelected(world, TileId, mapParent);
                    }

                    return;
                }
            }

            MapPreviewWindow.Instance?.Close();
        }
    }

    public static bool ShouldPreviewForTile(Tile tile, int tileId, MapParent mapParent)
    {
        if (tile.biome.impassable || tile.hilliness == Hilliness.Impassable)
        {
            if (!TileFinder.IsValidTileForNewSettlement(tileId)) return false;
        }

        if (mapParent != null)
        {
            var genDef = mapParent.MapGeneratorDef;
            if (genDef?.genSteps == null) return false;
            if (genDef.genSteps.Count(MapPreviewRequest.DefaultGenStepFilter) < 2) return false;
        }

        return true;
    }

    public static void Refresh() => Refresh(false);

    public static void Refresh(bool autoOpen)
    {
        TileId = autoOpen ? -2 : -1;
        if (!MapPreviewMod.Settings.EnableMapPreview)
        {
            MapPreviewWindow.Instance?.Close();
            MapPreviewToolbar.Instance?.Close();
        }
        else if (!MapPreviewMod.Settings.EnableToolbar)
        {
            MapPreviewToolbar.Instance?.Close();
        }
    }
}
