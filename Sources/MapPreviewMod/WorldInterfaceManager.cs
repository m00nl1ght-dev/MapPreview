using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace MapPreview;

public static class WorldInterfaceManager
{
    internal static int TileId = -1;

    private static bool _activeSinceEnteringMap;
    private static bool _openedPreviewSinceEnteringMap;

    static WorldInterfaceManager() => MapPreviewAPI.OnWorldChanged += Refresh;

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(WorldInterfaceManager));

    internal static void Update(WorldSelector selector)
    {
        if (!WorldRendererUtility.WorldRenderedNow)
        {
            if (_activeSinceEnteringMap)
            {
                MapPreviewWindow.Instance?.Close();
                MapPreviewToolbar.Instance?.Close();

                if (_openedPreviewSinceEnteringMap)
                {
                    MapPreviewAPI.UnsubscribeGenPatchesFast(PatchGroupSubscriber);
                    _openedPreviewSinceEnteringMap = false;
                }

                _activeSinceEnteringMap = false;
                
                TileId = -1;
            }

            return;
        }

        var selectedTileNow = selector.selectedTile;
        if (selectedTileNow < 0 && selector.NumSelectedObjects == 1)
        {
            var selectedObject = selector.SelectedObjects[0];
            if (selectedObject is MapParent)
            {
                selectedTileNow = selectedObject.Tile;
            }
        }

        if (TileId != selectedTileNow && MapPreviewMod.ActivePatchGroup.Active)
        {
            var wasAutoSelect = Current.ProgramState == ProgramState.Playing && TileId == -1 && !_activeSinceEnteringMap;

            TileId = selectedTileNow;

            if (TileId >= 0)
            {
                var world = Find.World;
                var worldTile = world.grid[TileId];
                var mapParent = world.worldObjects.MapParentAt(TileId);
                
                _activeSinceEnteringMap = true;

                if (MapPreviewMod.Settings.ToolbarEnabledNow)
                {
                    var toolbar = MapPreviewToolbar.Instance;
                    if (toolbar == null) Find.WindowStack.Add(toolbar = new MapPreviewToolbar());
                    toolbar.OnWorldTileSelected(world, TileId, mapParent);
                }

                if (ShouldPreviewForTile(worldTile, TileId, mapParent) && (!wasAutoSelect || MapPreviewMod.Settings.AutoOpenPreviewOnWorldMap))
                {
                    if (MapPreviewMod.Settings.PreviewEnabledNow && MapPreviewAPI.IsReady)
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

    internal static void RefreshActive()
    {
        bool preview = MapPreviewMod.Settings.PreviewEnabledNow;
        bool toolbar = MapPreviewMod.Settings.ToolbarEnabledNow;
        
        if (!preview)
        {
            MapPreviewWindow.Instance?.Close();
            MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
            _openedPreviewSinceEnteringMap = false;
        }
        
        if (!toolbar)
        {
            MapPreviewToolbar.Instance?.Close();
        }
        
        if (preview || toolbar)
        {
            MapPreviewMod.ActivePatchGroup.Subscribe();
        }
        else
        {
            MapPreviewMod.ActivePatchGroup.Unsubscribe();
            _activeSinceEnteringMap = false;
        }
        
        Refresh();
    }

    public static void Refresh(bool autoOpen)
    {
        TileId = autoOpen ? -2 : -1;
    }
    
    public static void Refresh()
    {
        TileId = -1;
    }
}
