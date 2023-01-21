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
    private static bool _openedPreviewSinceEnteringMap;

    static Patch_RimWorld_WorldInterface() => MapPreviewAPI.OnWorldChanged += Refresh;
    
    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(Patch_RimWorld_WorldInterface));
    
    [HarmonyPostfix]
    [HarmonyPatch("WorldInterfaceUpdate")]
    private static void WorldInterfaceUpdate(WorldInterface __instance)
    {
        if (!WorldRendererUtility.WorldRenderedNow)
        {
            if (_openedPreviewSinceEnteringMap)
            {
                MapPreviewWindow.Instance?.Close();
                MapPreviewToolbar.Instance?.Close();
                MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
                _openedPreviewSinceEnteringMap = false;
                _tileId = -1;
            }
            return;
        }
        
        if (_tileId != __instance.SelectedTile)
        {
            _tileId = __instance.SelectedTile;

            if (_tileId != -1)
            {
                var tile = Find.World.grid[_tileId];
                if (ShouldPreviewForTile(tile, _tileId))
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
                    
                    if (MapPreviewMod.Settings.EnableToolbar)
                    {
                        var toolbar = MapPreviewToolbar.Instance;
                        if (toolbar == null) Find.WindowStack.Add(toolbar = new MapPreviewToolbar());
                        toolbar.OnWorldTileSelected(Find.World, _tileId);
                    }

                    return;
                }
            }
            
            MapPreviewToolbar.Instance?.OnWorldTileSelected(Find.World, _tileId);
            MapPreviewWindow.Instance?.Close();
        }
    }

    public static bool ShouldPreviewForTile(Tile tile, int tileId)
    {
        return !tile.biome.impassable && (tile.hilliness != Hilliness.Impassable || TileFinder.IsValidTileForNewSettlement(tileId));
    }

    public static void Refresh()
    {
        _tileId = -1;
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