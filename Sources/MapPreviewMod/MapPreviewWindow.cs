using System;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewWindow : Window
{
    public static IntVec2 MinMapSize = new(10, 10);
    public static IntVec2 MaxMapSize = new(500, 500);

    public static Func<IntVec2> MapSizeOverride;
    
    public static MapPreviewWindow Instance => Find.WindowStack?.WindowOfType<MapPreviewWindow>();

    public Vector2 DefaultPos => new(UI.screenWidth - InitialSize.x - 50f, 105f);
    public override Vector2 InitialSize => new(MapPreviewMod.Settings.PreviewWindowSize, MapPreviewMod.Settings.PreviewWindowSize);
    protected override float Margin => 0f;

    private readonly MapPreviewWidgetWithPreloader _previewWidget = new(MaxMapSize);

    public Map CurrentPreviewMap => _previewWidget.PreviewMap;

    public MapPreviewWindow()
    {
        layer = WindowLayer.SubSuper;
        closeOnCancel = false;
        doCloseButton = false;
        doCloseX = false;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
        preventCameraMotion = false;
        forcePause = false;
        resizeable = false;
        draggable = !MapPreviewMod.Settings.LockWindowPositions;
    }

    public void OnWorldTileSelected(World world, int tileId)
    {
        MapPreviewGenerator.Instance.ClearQueue();

        if (!MapPreviewAPI.IsReadyForPreviewGen)
        {
            Close();
            return;
        }
        
        int seed = SeedRerollData.GetMapSeed(world, tileId);
        var mapSize = DetermineMapSize(world, tileId);
        
        mapSize = new IntVec2(Mathf.Clamp(mapSize.x, MinMapSize.x, MaxMapSize.x), Mathf.Clamp(mapSize.z, MinMapSize.z, MaxMapSize.z));

        float desiredSize = MapPreviewMod.Settings.PreviewWindowSize;
        float largeSide = Math.Max(mapSize.x, mapSize.z);
        float scale = desiredSize / largeSide;

        windowRect = new Rect(windowRect.x, windowRect.y, mapSize.x * scale, mapSize.z * scale);
        windowRect = windowRect.Rounded();

        var request = new MapPreviewRequest(seed, tileId, mapSize)
        {
            TextureSize = new IntVec2(_previewWidget.Texture.width, _previewWidget.Texture.height),
            UseTrueTerrainColors = MapPreviewMod.Settings.EnableTrueTerrainColors,
            SkipRiverFlowCalc = MapPreviewMod.Settings.SkipRiverFlowCalc,
            ExistingBuffer = _previewWidget.Buffer
        };

        MapPreviewGenerator.Instance.QueuePreviewRequest(request);
        _previewWidget.Await(request);
        
        var pos = new Vector2((int) windowRect.x, (int) windowRect.y);
        if (pos != MapPreviewMod.Settings.PreviewWindowPos)
        {
            MapPreviewMod.Settings.PreviewWindowPos = pos;
            MapPreviewMod.Settings.Write();
        }
    }

    public static IntVec2 DetermineMapSize(World world, int tileId)
    {
        var mapParent = world.worldObjects.MapParentAt(tileId);
        if (mapParent is Site site)
        {
            var fromSite = site.PreferredMapSize;
            return new IntVec2(fromSite.x, fromSite.z);
        }
        
        if (Current.Game.Maps.Any())
        {
            var fromWorld = world.info.initialMapSize;
            return new IntVec2(fromWorld.x, fromWorld.z);
        }
        
        if (MapSizeOverride != null)
        {
            var fromOverride = MapSizeOverride.Invoke();
            if (fromOverride.x > 0 && fromOverride.z > 0)
            {
                return new IntVec2(fromOverride.x, fromOverride.z);
            }
        }

        var gameInitData = Find.GameInitData;
        if (gameInitData != null)
        {
            return new IntVec2(gameInitData.mapSize, gameInitData.mapSize);
        }
        
        return new IntVec2(250, 250);
    }

    public void ResetPositionAndSize()
    {
        windowRect.size = InitialSize;
        windowRect.position = DefaultPos;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        
        if (Instance != this) Instance?.Close();
        
        MapPreviewGenerator.Init();
        
        float lastX = MapPreviewMod.Settings.PreviewWindowPos.x;
        float lastY = MapPreviewMod.Settings.PreviewWindowPos.y;
        
        windowRect.x = lastX >= 0 ? lastX : DefaultPos.x;
        windowRect.y = lastY >= 0 ? lastY : DefaultPos.y;
        
        if (windowRect.x + windowRect.width > UI.screenWidth) windowRect.x = DefaultPos.x;
        if (windowRect.y + windowRect.height > UI.screenHeight) windowRect.y = DefaultPos.y;
    }

    public override void PreClose()
    {
        base.PreClose();
        
        _previewWidget.Dispose();
        
        MapPreviewGenerator.Instance.ClearQueue();
        
        var pos = new Vector2((int)windowRect.x, (int)windowRect.y);
        if (pos != MapPreviewMod.Settings.PreviewWindowPos)
        {
            MapPreviewMod.Settings.PreviewWindowPos = pos;
            MapPreviewMod.Settings.Write();
        }
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        _previewWidget.Draw(inRect.ContractedBy(5f));
    }
}