using MapReroll;
using MapReroll.Promises;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewWindow : Window
{
    public static MapPreviewWindow Instance { get; private set; }
    
    public override Vector2 InitialSize => new(ModInstance.Settings.PreviewWindowSize, ModInstance.Settings.PreviewWindowSize);
    protected override float Margin => 0f;

    private static float _lastX = -1, _lastY = -1;
    
    private readonly MapPreviewGenerator _previewGenerator;
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
        _previewGenerator = new MapPreviewGenerator();
        if (Instance != this) Instance?.Close();
        Instance = this;
    }

    public void OnWorldTileSelected(World world, int tileId)
    {
        _preview?.Dispose();
        string seed = world.info.seedString;
        IPromise<Texture2D> promise = _previewGenerator.QueuePreviewForSeed(seed, tileId, world.info.initialMapSize.x, true);
        _preview = new MapPreview(promise, seed);
    }

    public override void PreOpen()
    {
        base.PreOpen();
        windowRect.x = _lastX >= 0 ? _lastX : _lastX = UI.screenWidth - InitialSize.x - 50f;
        windowRect.y = _lastY >= 0 ? _lastY : _lastY = 100f;
    }

    public override void PreClose()
    {
        base.PreClose();
        _previewGenerator.Dispose();
        _preview.Dispose();
        _preview = null;
        _lastX = windowRect.x;
        _lastY = windowRect.y;
        if (Instance == this) Instance = null;
    }
    
    public override void DoWindowContents(Rect inRect)
    {
        _preview?.Draw(inRect.ContractedBy(5f), 0, false);
    }
}