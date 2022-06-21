using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[HarmonyPatch(typeof(WorldInterface))]
public class RimWorld_WorldInterface
{
    private static int _tileId = -1;
    private static bool _openedPreviewSinceEnteringMap;
    
    [HarmonyPatch("WorldInterfaceUpdate")]
    private static void Postfix(WorldInterface __instance)
    {
        if (!WorldRendererUtility.WorldRenderedNow)
        {
            if (_openedPreviewSinceEnteringMap)
            {
                MapPreviewWindow.Instance?.Close();
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
                if (!tile.biome.impassable && (tile.hilliness != Hilliness.Impassable || TileFinder.IsValidTileForNewSettlement(_tileId)))
                {
                    if (!ModInstance.Settings.EnableMapPreview) return;
                    var window = MapPreviewWindow.Instance;
                    if (window == null) Find.WindowStack.Add(window = new MapPreviewWindow());
                    window.OnWorldTileSelected(Find.World, _tileId);
                    _openedPreviewSinceEnteringMap = true;
                    return;
                }
            }
            
            MapPreviewWindow.Instance?.Close();
        }
    }

    public static void Refresh()
    {
        _tileId = -1;
        if (!ModInstance.Settings.EnableMapPreview) MapPreviewWindow.Instance?.Close();
    }
}