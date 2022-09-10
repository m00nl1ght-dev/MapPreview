using LunarFramework.Patching;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewWindow : Window
{
    public const int MaxMapSize = 500;
    
    public static MapPreviewWindow Instance => Find.WindowStack?.WindowOfType<MapPreviewWindow>();
    
    public override Vector2 InitialSize => new(ModInstance.Settings.PreviewWindowSize, ModInstance.Settings.PreviewWindowSize);
    protected override float Margin => 0f;

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(MapPreviewWindow));

    private readonly BasicMapPreview _preview = new(MaxMapSize);

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
        draggable = true;
    }

    public void OnWorldTileSelected(World world, int tileId)
    {
        MapPreviewGenerator.Instance.ClearQueue();

        if (!Main.IsReadyForPreviewGen)
        {
            Close();
            return;
        }
        
        string seed = world.info.seedString;
        int mapSize = DetermineMapSize(world);

        var trueTerrainColors = ModInstance.Settings.EnableTrueTerrainColors;
        var promise = MapPreviewGenerator.Instance.QueuePreviewForSeed(seed, tileId, mapSize, MaxMapSize, trueTerrainColors, _preview.Buffer);
        _preview.Await(promise, tileId);
        
        var pos = new Vector2((int) windowRect.x, (int) windowRect.y);
        if (pos != ModInstance.Settings.PreviewWindowPosition)
        {
            ModInstance.Settings.PreviewWindowPosition = pos;
            ModInstance.Settings.Write();
        }
    }

    private int DetermineMapSize(World world)
    {
        var gameInitData = Find.GameInitData;
        if (gameInitData is { mapSize: >= 100 and <= 1000 }) return gameInitData.mapSize;
        var fromWorld = world.info.initialMapSize.x;
        if (fromWorld is >= 100 and <= 1000) return fromWorld;
        return 250;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        
        if (Instance != this) Instance?.Close();
        
        MapPreviewGenerator.Init();
        
        Main.SubscribeGenPatches(PatchGroupSubscriber);
        
        float lastX = ModInstance.Settings.PreviewWindowPosition.x;
        float lastY = ModInstance.Settings.PreviewWindowPosition.y;
        
        windowRect.x = lastX >= 0 ? lastX : UI.screenWidth - InitialSize.x - 50f;
        windowRect.y = lastY >= 0 ? lastY : 100f;
        
        if (windowRect.x + windowRect.width > UI.screenWidth) windowRect.x = UI.screenWidth - InitialSize.x - 50f;
        if (windowRect.y + windowRect.height > UI.screenHeight) windowRect.y = 100f;
    }

    public override void PreClose()
    {
        base.PreClose();
        
        _preview.Dispose();
        
        MapPreviewGenerator.Instance.ClearQueue();
        
        Main.UnsubscribeGenPatches(PatchGroupSubscriber);
        
        var pos = new Vector2((int)windowRect.x, (int)windowRect.y);
        if (pos != ModInstance.Settings.PreviewWindowPosition)
        {
            ModInstance.Settings.PreviewWindowPosition = pos;
            ModInstance.Settings.Write();
        }
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        _preview.Draw(inRect.ContractedBy(5f));
    }
}