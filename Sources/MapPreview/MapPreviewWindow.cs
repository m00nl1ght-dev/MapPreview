using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewWindow : Window
{
    public static MapPreviewWindow Instance => Find.WindowStack?.WindowOfType<MapPreviewWindow>();
    
    public override Vector2 InitialSize => new(ModInstance.Settings.PreviewWindowSize, ModInstance.Settings.PreviewWindowSize);
    protected override float Margin => 0f;

    private static ExactMapPreviewGenerator _exactPreviewGenerator;
    private MapPreview _preview;

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
        _exactPreviewGenerator ??= new ExactMapPreviewGenerator();
        if (Instance != this) Instance?.Close();
    }

    public void OnWorldTileSelected(World world, int tileId)
    {
        _preview?.Dispose();
        _exactPreviewGenerator.ClearQueue();
        string seed = world.info.seedString;
        var promise = _exactPreviewGenerator.QueuePreviewForSeed(seed, tileId, world.info.initialMapSize.x);
        _preview = new MapPreview(promise, seed);
        var pos = new Vector2((int)windowRect.x, (int)windowRect.y);
        if (pos != ModInstance.Settings.PreviewWindowPosition)
        {
            ModInstance.Settings.PreviewWindowPosition = pos;
            ModInstance.Settings.Write();
        }
    }

    public override void PreOpen()
    {
        base.PreOpen();
        
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
        _preview = null;
        var pos = new Vector2((int)windowRect.x, (int)windowRect.y);
        if (pos != ModInstance.Settings.PreviewWindowPosition)
        {
            ModInstance.Settings.PreviewWindowPosition = pos;
            ModInstance.Settings.Write();
        }
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        _preview?.Draw(inRect.ContractedBy(5f), 0, false);
    }

    public static void Dispose()
    {
        if (_exactPreviewGenerator != null)
        {
            _exactPreviewGenerator.Dispose();
            _exactPreviewGenerator.WaitForDisposal();
            _exactPreviewGenerator = null;
        }
    }
}