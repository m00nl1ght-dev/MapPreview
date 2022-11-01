using System;
using System.Reflection;
using HarmonyLib;
using LunarFramework.GUI;
using MapPreview.Patches;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewToolbar : Window
{
    private const float MinWidth = 200f;
    
    public static MapPreviewToolbar Instance => Find.WindowStack?.WindowOfType<MapPreviewToolbar>();
    
    public Vector2 DefaultPos => new(UI.screenWidth - MapPreviewMod.Settings.PreviewWindowSize - 50f, 50f);
    public override Vector2 InitialSize => new(Math.Max(MapPreviewMod.Settings.PreviewWindowSize, MinWidth), 50);
    protected override float Margin => 0f;
    
    public static event Action<MapPreviewToolbar, LayoutRect> ExtToolbar;
    
    private bool _currentTileCanBeRerolled;
    private bool _currentTileIsRerolled;
    
    public MapPreviewToolbar()
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
        if (tileId >= 0 && Patch_RimWorld_WorldInterface.ShouldPreviewForTile(world.grid[tileId], tileId))
        {
            var rerollData = world.GetComponent<SeedRerollData>();
            var mapParent = Find.WorldObjects.MapParentAt(tileId);
            _currentTileIsRerolled = rerollData.TryGet(tileId, out _);
            _currentTileCanBeRerolled = mapParent is not { HasMap: true };
        }
        else
        {
            _currentTileIsRerolled = false;
            _currentTileCanBeRerolled = false;
        }
    }

    public override void PreOpen()
    {
        base.PreOpen();
        
        if (Instance != this) Instance?.Close();

        float lastX = MapPreviewMod.Settings.ToolbarWindowPos.x;
        float lastY = MapPreviewMod.Settings.ToolbarWindowPos.y;

        windowRect.x = lastX >= 0 ? lastX : DefaultPos.x;
        windowRect.y = lastY >= 0 ? lastY : DefaultPos.y;
        
        if (windowRect.x + windowRect.width > UI.screenWidth) windowRect.x = DefaultPos.x;
        if (windowRect.y + windowRect.height > UI.screenHeight) windowRect.y = DefaultPos.y;
    }

    public override void PreClose()
    {
        base.PreClose();

        var pos = new Vector2((int)windowRect.x, (int)windowRect.y);
        if (pos != MapPreviewMod.Settings.ToolbarWindowPos)
        {
            MapPreviewMod.Settings.ToolbarWindowPos = pos;
            MapPreviewMod.Settings.Write();
        }
    }

    private readonly LayoutRect _layout = new(MapPreviewMod.LunarAPI);
    
    private Texture2D _btnSettingsTex;
    private Texture2D _btnRerollWorldTex;

    private MethodInfo _wpCanDoNext;
    private FieldInfo _wpSeedString;

    public override void DoWindowContents(Rect inRect)
    {
        _layout.BeginRoot(inRect, new LayoutParams { Margin = new(10), Horizontal = true });
        
        _layout.BeginRel(0.5f, new LayoutParams { Spacing = 10, DefaultSize = 30, Horizontal = true });

        var rerollAllowed = MapPreviewMod.Settings.EnableSeedRerollFeature || _currentTileIsRerolled;

        if (rerollAllowed)
        {
            var canReroll = !MapPreviewAPI.IsGeneratingPreview && _currentTileCanBeRerolled;

            GUI.enabled = canReroll;
            var rerollPos = _layout.Abs();
            TooltipHandler.TipRegion(rerollPos, "MapPreview.World.RerollMapSeed".Translate());
            if (GUI.Button(rerollPos, MapPreviewWidgetWithPreloader.UIPreviewLoading))
            {
                var tile = Find.WorldSelector.selectedTile;
                var rerollData = Find.World.GetComponent<SeedRerollData>();
                var seed = rerollData.TryGet(tile, out var savedSeed) ? savedSeed : SeedRerollData.GetOriginalMapSeed(Find.World, tile);
                unchecked { seed += 1; }
                rerollData.Commit(tile, seed);
            }

            GUI.enabled = _currentTileIsRerolled && !MapPreviewAPI.IsGeneratingPreview && _currentTileCanBeRerolled;
            var resetPos = _layout.Abs();
            TooltipHandler.TipRegion(resetPos, "MapPreview.World.ResetMapSeed".Translate());
            if (GUI.Button(resetPos, MapPreviewWidgetWithPreloader.UIPreviewReset))
            {
                var tile = Find.WorldSelector.selectedTile;
                var rerollData = Find.World.GetComponent<SeedRerollData>();
                rerollData.Reset(tile);
            }
        }

        GUI.enabled = true;
        ExtToolbar?.Invoke(this, _layout);
        
        if (Current.ProgramState == ProgramState.Entry)
        {
            GUI.enabled = !MapPreviewAPI.IsGeneratingPreview;
            var rerollWorldPos = _layout.Abs();
            TooltipHandler.TipRegion(rerollWorldPos, "MapPreview.World.RerollWorldSeed".Translate());
            if (GUI.Button(rerollWorldPos, _btnRerollWorldTex ??= ContentFinder<Texture2D>.Get("RerollWorldSeedMP")))
            {
                var windowStack = Find.WindowStack;
                var page = windowStack.WindowOfType<Page_SelectStartingSite>();
                if (page is { prev: Page_CreateWorldParams paramsPage })
                {
                    _wpCanDoNext ??= AccessTools.Method(typeof(Page_CreateWorldParams), "CanDoNext");
                    _wpSeedString ??= AccessTools.Field(typeof(Page_CreateWorldParams), "seedString");
                    
                    windowStack.Add(paramsPage);
                    page.Close();
                    
                    windowStack.WindowOfType<WorldInspectPane>()?.Close();

                    _wpSeedString?.SetValue(paramsPage, GenText.RandomSeedString());
                    _wpCanDoNext?.Invoke(paramsPage, Array.Empty<object>());
                }
            }
        }
        
        GUI.enabled = true;
        
        _layout.End();
        
        _layout.BeginRel(0.5f, new LayoutParams { Spacing = 10, DefaultSize = 30, Horizontal = true, Reversed = true });
        
        var settingsPos = _layout.Abs();
        TooltipHandler.TipRegion(settingsPos, "MapPreview.World.OpenSettings".Translate());
        if (GUI.Button(settingsPos, _btnSettingsTex ??= ContentFinder<Texture2D>.Get(OptionCategoryDefOf.General.texPath)))
        {
            var windowStack = Find.WindowStack;
            var existing = windowStack.WindowOfType<Dialog_ModSettings>();
            
            if (existing != null)
            {
                existing.Close();
            }
            else
            {
                windowStack.Add(new Dialog_ModSettings(MapPreviewMod.Settings.Mod));
            }
        }
        
        _layout.End();

        _layout.End();
    }
}