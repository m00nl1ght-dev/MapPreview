using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(WorldInterface))]
internal class Patch_RimWorld_WorldInterface
{
    private static int _tileId = -1;
    private static bool _activeSinceEnteringMap;
    private static bool _openedPreviewSinceEnteringMap;

    static Patch_RimWorld_WorldInterface() => MapPreviewAPI.OnWorldChanged += Refresh;
    
    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(Patch_RimWorld_WorldInterface));
    
    [HarmonyPostfix]
    [HarmonyPatch("WorldInterfaceUpdate")]
    private static void WorldInterfaceUpdate(WorldInterface __instance)
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
                _tileId = -1;
            }
            return;
        }
        
        if (_tileId != __instance.SelectedTile)
        {
            var wasAutoSelect = Current.ProgramState == ProgramState.Playing && _tileId == -1 && !_openedPreviewSinceEnteringMap;
            
            _tileId = __instance.SelectedTile;

            if (_tileId >= 0)
            {
                var tile = Find.World.grid[_tileId];
                _activeSinceEnteringMap = true;
                
                if (MapPreviewMod.Settings.EnableToolbar)
                {
                    var toolbar = MapPreviewToolbar.Instance;
                    if (toolbar == null) Find.WindowStack.Add(toolbar = new MapPreviewToolbar());
                    toolbar.OnWorldTileSelected(Find.World, _tileId);
                }
                
                if (ShouldPreviewForTile(tile, _tileId) && (!wasAutoSelect || MapPreviewMod.Settings.AutoOpenPreviewOnWorldMap))
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
                        window.OnWorldTileSelected(Find.World, _tileId);
                    }

                    return;
                }
            }
            
            MapPreviewWindow.Instance?.Close();
        }
    }

    public static bool ShouldPreviewForTile(Tile tile, int tileId)
    {
        return !tile.biome.impassable && (tile.hilliness != Hilliness.Impassable || TileFinder.IsValidTileForNewSettlement(tileId));
    }

    public static void Refresh() => Refresh(false);

    public static void Refresh(bool autoOpen)
    {
        _tileId = autoOpen ? -2 : -1;
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