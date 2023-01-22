using System.Collections.Generic;
using LunarFramework.GUI;
using LunarFramework.Utility;
using MapPreview.Compatibility;
using MapPreview.Patches;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewSettings : ModSettings
{
    private const float DefaultPreviewWindowSize = 250f;

    public float PreviewWindowSize = DefaultPreviewWindowSize;
    public bool EnableTrueTerrainColors = true;
    public bool EnableMapPreview = true;
    public bool EnableToolbar = true;
    public bool LockWindowPositions;
    public bool EnableSeedRerollFeature;
    public bool EnableWorldSeedRerollFeature;
    public bool SkipRiverFlowCalc = true;
    public bool EnableMapDesignerIntegration = true;
    public bool EnablePrepareLandingIntegration = true;
    public bool EnableWorldEditIntegration = true;
    
    public Vector2 PreviewWindowPos = new(-1, -1);
    public Vector2 ToolbarWindowPos = new(-1, -1);

    private readonly LayoutRect _layout = new(MapPreviewMod.LunarAPI);
    
    private Tab _tab = Tab.Preview;
    private List<TabRecord> _tabs;
    private Vector2 _scrollPos;
    private Rect _viewRect;

    public void DoSettingsWindowContents(Rect rect)
    {
        if (!MapPreviewMod.LunarAPI.IsInitialized())
        {
            _layout.BeginRoot(rect);
            LunarGUI.Label(_layout, "An error occured whie loading this mod. Check the log file for more information.");
            _layout.End();
            return;
        }
        
        _tabs ??= new()
        {
            new("MapPreview.Settings.Tab.Preview".Translate(), () => _tab = Tab.Preview, () => _tab == Tab.Preview),
            new("MapPreview.Settings.Tab.Toolbar".Translate(), () => _tab = Tab.Toolbar, () => _tab == Tab.Toolbar)
        };
        
        rect.yMin += 35;
        rect.yMax -= 12;
        
        Widgets.DrawMenuSection(rect);
        TabDrawer.DrawTabs(rect, _tabs);
        
        rect = rect.ContractedBy(18f);
        
        switch (_tab)
        {
            case Tab.Preview: DoPreviewSettingsTab(rect); break;
            case Tab.Toolbar: DoToolbarSettingsTab(rect); break;
        }
    }
    
    public void DoPreviewSettingsTab(Rect rect)
    {
        LunarGUI.BeginScrollView(rect, ref _viewRect, ref _scrollPos);
        
        _layout.BeginRoot(_viewRect, new LayoutParams { Spacing = 10 });
        
        LunarGUI.PushChanged();
        
        LunarGUI.Checkbox(_layout, ref EnableMapPreview, "MapPreview.Settings.EnableMapPreview".Translate());

        _layout.Abs(10f);
        
        LunarGUI.PushEnabled(EnableMapPreview);
        
        LunarGUI.Checkbox(_layout, ref EnableTrueTerrainColors, "MapPreview.Settings.EnableTrueTerrainColors".Translate());
        LunarGUI.Checkbox(_layout, ref SkipRiverFlowCalc, "MapPreview.Settings.SkipRiverFlowCalc".Translate());
        
        if (LunarGUI.PopChanged())
        {
            Patch_RimWorld_WorldInterface.Refresh();
        }

        _layout.Abs(10f);
        
        LunarGUI.PushChanged();
        
        LunarGUI.Checkbox(_layout, ref LockWindowPositions, "MapPreview.Settings.LockWindowPositions".Translate());
        
        if (LunarGUI.PopChanged())
        {
            var previewWindow = MapPreviewWindow.Instance;
            if (previewWindow != null) previewWindow.draggable = !LockWindowPositions;
            var toolbarWindow = MapPreviewToolbar.Instance;
            if (toolbarWindow != null) toolbarWindow.draggable = !LockWindowPositions;
        }
        
        _layout.Abs(10f);
        
        LunarGUI.PushChanged();

        LunarGUI.LabelDouble(_layout, "MapPreview.Settings.PreviewWindowSize".Translate(), PreviewWindowSize.ToString("F0"));
        LunarGUI.Slider(_layout, ref PreviewWindowSize, 100f, 800f);
        
        if (LunarGUI.PopChanged())
        {
            PreviewWindowPos = new Vector2(-1, -1);
            ToolbarWindowPos = new Vector2(-1, -1);

            MapPreviewWindow.Instance?.ResetPositionAndSize();
            MapPreviewToolbar.Instance?.ResetPositionAndSize();
        }

        LunarGUI.PopEnabled();
        
        _layout.Abs(10f);
        
        if (Prefs.DevMode && LunarGUI.Button(_layout, "[DEV] Clear cache and recalculate all terrain colors"))
        {
            TrueTerrainColors.CalculateTrueTerrainColors(true);
            Patch_RimWorld_WorldInterface.Refresh();
        }

        _viewRect.height = _layout.OccupiedSpace;
        
        _layout.End();
        
        LunarGUI.EndScrollView();
    }
    
    public void DoToolbarSettingsTab(Rect rect)
    {
        LunarGUI.BeginScrollView(rect, ref _viewRect, ref _scrollPos);
        
        _layout.BeginRoot(_viewRect, new LayoutParams { Spacing = 10 });
        
        LunarGUI.PushChanged();
        
        LunarGUI.Checkbox(_layout,  ref EnableToolbar, "MapPreview.Settings.EnableToolbar".Translate());
        
        if (LunarGUI.PopChanged())
        {
            Patch_RimWorld_WorldInterface.Refresh();
        }

        _layout.Abs(10f);

        LunarGUI.PushEnabled(EnableToolbar && !ModCompat_MapReroll.IsPresent);
        
        var trEntry = "MapPreview.Settings.EnableSeedRerollFeature";
        if (ModCompat_MapReroll.IsPresent) trEntry += ".MapRerollConflict";
        LunarGUI.Checkbox(_layout, ref EnableSeedRerollFeature, trEntry.Translate());

        LunarGUI.PopEnabled();
        LunarGUI.PushEnabled(EnableToolbar);

        LunarGUI.Checkbox(_layout, ref EnableWorldSeedRerollFeature, "MapPreview.Settings.EnableWorldSeedRerollFeature".Translate());

        if (ModCompat_MapDesigner.IsPresent)
        {
            LunarGUI.Checkbox(_layout, ref EnableMapDesignerIntegration, "MapPreview.Integration.MapDesigner.Enabled".Translate());
        }
        
        if (ModCompat_PrepareLanding.IsPresent)
        {
            LunarGUI.Checkbox(_layout, ref EnablePrepareLandingIntegration, "MapPreview.Integration.PrepareLanding.Enabled".Translate());
        }
        
        if (ModCompat_WorldEdit.IsPresent)
        {
            LunarGUI.Checkbox(_layout, ref EnableWorldEditIntegration, "MapPreview.Integration.WorldEdit.Enabled".Translate());
        }
        
        LunarGUI.PopEnabled();

        _viewRect.height = _layout.OccupiedSpace;
        
        _layout.End();
        
        LunarGUI.EndScrollView();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PreviewWindowSize, "PreviewWindowSize", DefaultPreviewWindowSize);
        Scribe_Values.Look(ref EnableTrueTerrainColors, "EnableTrueTerrainColors", true);
        Scribe_Values.Look(ref EnableMapPreview, "EnableMapPreview", true);
        Scribe_Values.Look(ref EnableToolbar, "EnableToolbar", true);
        Scribe_Values.Look(ref LockWindowPositions, "LockWindowPositions");
        Scribe_Values.Look(ref EnableSeedRerollFeature, "EnableSeedRerollFeature");
        Scribe_Values.Look(ref EnableWorldSeedRerollFeature, "EnableWorldSeedRerollFeature", true);
        Scribe_Values.Look(ref SkipRiverFlowCalc, "SkipRiverFlowCalc", true);
        Scribe_Values.Look(ref EnableMapDesignerIntegration, "EnableMapDesignerIntegration", true);
        Scribe_Values.Look(ref EnablePrepareLandingIntegration, "EnablePrepareLandingIntegration", true);
        Scribe_Values.Look(ref EnableWorldEditIntegration, "EnableWorldEditIntegration", true);
        Scribe_Values.Look(ref PreviewWindowPos, "PreviewWindowPos", new Vector2(-1, -1));
        Scribe_Values.Look(ref ToolbarWindowPos, "ToolbarWindowPos", new Vector2(-1, -1));
        
        base.ExposeData();

        if (ToolbarWindowPos == new Vector2(-1, -1))
        {
            PreviewWindowPos = new Vector2(-1, -1);
        }
    }

    public void ResetAll()
    {
        PreviewWindowSize = DefaultPreviewWindowSize;
        EnableTrueTerrainColors = true;
        EnableMapPreview = true;
        EnableToolbar = true;
        LockWindowPositions = false;
        EnableSeedRerollFeature = false;
        EnableWorldSeedRerollFeature = true;
        SkipRiverFlowCalc = true;
        EnableMapDesignerIntegration = true;
        EnablePrepareLandingIntegration = true;
        EnableWorldEditIntegration = true;
        PreviewWindowPos = new Vector2(-1, -1);
        ToolbarWindowPos = new Vector2(-1, -1);
    }
    
    public enum Tab
    {
        Preview, Toolbar
    }
}