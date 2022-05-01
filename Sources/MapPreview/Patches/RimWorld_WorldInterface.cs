using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(WorldInterface))]
public class RimWorld_WorldInterface
{
    private static int _tileId = -1;
    
    [HarmonyPatch("WorldInterfaceUpdate")]
    private static void Postfix(WorldInterface __instance)
    {
        if (!WorldRendererUtility.WorldRenderedNow)
        {
            MapPreviewWindow.Instance?.Close();
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
                    if (MapPreviewWindow.Instance == null) Find.WindowStack.Add(new MapPreviewWindow());
                    MapPreviewWindow.Instance.OnWorldTileSelected(Find.World, _tileId);
                    return;
                }
            }
            
            MapPreviewWindow.Instance?.Close();
        }
    }
}