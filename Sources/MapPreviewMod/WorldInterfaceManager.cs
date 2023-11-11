using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace MapPreview;

public static class WorldInterfaceManager
{
    internal static int TileId = -1;

    private static int _framesActive;

    private static bool _activeSinceEnteringMap;
    private static bool _openedPreviewSinceEnteringMap;

    private static bool PreviewEnabledNow => MapPreviewMod.Settings.PreviewEnabledNow;
    private static bool ToolbarEnabledNow => MapPreviewMod.Settings.ToolbarEnabledNow;
    
    private static bool PreviewAutoOpen => MapPreviewMod.Settings.AutoOpenPreviewOnWorldMap;
    private static bool PreviewWorldObj => MapPreviewMod.Settings.TriggerPreviewOnWorldObjects;

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(WorldInterfaceManager));

    static WorldInterfaceManager() => MapPreviewAPI.OnWorldChanged += RefreshPreview;
    
    public static void RefreshPreview() => TileId = -1;

    public static void RefreshInterface()
    {
        if (!PreviewEnabledNow)
        {
            MapPreviewWindow.Instance?.Close();
            MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
            _openedPreviewSinceEnteringMap = false;
        }
        
        if (!ToolbarEnabledNow)
        {
            MapPreviewToolbar.Instance?.Close();
        }
        
        if (PreviewEnabledNow || ToolbarEnabledNow)
        {
            MapPreviewMod.ActivePatchGroup.Subscribe();
        }
        else
        {
            MapPreviewMod.ActivePatchGroup.Unsubscribe();
            _activeSinceEnteringMap = false;
        }
        
        TileId = -1;
    }

    public static bool ShouldPreviewForTile(Tile tile, int tileId, MapParent mapParent)
    {
        if (tile.biome.impassable || Find.World.Impassable(tileId))
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

    internal static void UpdateWhileWorldShown(WorldSelector selector)
    {
        var selectedTileNow = selector.selectedTile;
        
        if (selectedTileNow < 0 && selector.NumSelectedObjects == 1)
        {
            if (PreviewWorldObj)
            {
                var selectedObject = selector.SelectedObjects[0];
                
                if (selectedObject is MapParent)
                {
                    selectedTileNow = selectedObject.Tile;
                }
            }
        }

        if (!_activeSinceEnteringMap && MapPreviewMod.ActivePatchGroup.Active && !LongEventHandler.ShouldWaitForEvent)
        {
            _activeSinceEnteringMap = true;
            _framesActive = 0;
            UpdateToolbar();
        }

        if (_activeSinceEnteringMap)
        {
            _framesActive++;
            
            if (TileId != selectedTileNow)
            {
                var wasAutoSelect = TileId == -1 && _framesActive <= 5;

                TileId = selectedTileNow;

                if (TileId >= 0)
                {
                    var world = Find.World;
                    var worldTile = world.grid[TileId];
                    var mapParent = world.worldObjects.MapParentAt(TileId);

                    var toolbar = MapPreviewToolbar.Instance;
                    if (toolbar == null && ToolbarEnabledNow) Find.WindowStack.Add(toolbar = new MapPreviewToolbar());
                    toolbar?.OnWorldTileSelected(world, TileId, mapParent);

                    if (ShouldPreviewForTile(worldTile, TileId, mapParent) && (!wasAutoSelect || PreviewAutoOpen))
                    {
                        if (PreviewEnabledNow && MapPreviewAPI.IsReady)
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
                    }
                    else
                    {
                        MapPreviewWindow.Instance?.Close();
                    }
                }
                else
                {
                    MapPreviewWindow.Instance?.Close();
                }
            }
        }
    }

    internal static void UpdateWhileWorldHidden()
    {
        if (_activeSinceEnteringMap)
        {
            _activeSinceEnteringMap = false;
            
            MapPreviewWindow.Instance?.Close();
            MapPreviewToolbar.Instance?.Close();

            if (_openedPreviewSinceEnteringMap)
            {
                _openedPreviewSinceEnteringMap = false;
                MapPreviewAPI.UnsubscribeGenPatchesFast(PatchGroupSubscriber);
            }

            TileId = -1;
        }
    }
    
    internal static void UpdateToolbar()
    {
        var toolbar = MapPreviewToolbar.Instance;
        
        if (ToolbarEnabledNow && toolbar == null)
        {
            Find.WindowStack.Add(toolbar = new MapPreviewToolbar());
        }
        
        if (toolbar != null)
        {
            var mapParent = TileId >= 0 ? Find.WorldObjects.MapParentAt(TileId) : null;
            toolbar.OnWorldTileSelected(Find.World, TileId, mapParent);
        }
    }
}
