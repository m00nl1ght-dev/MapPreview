using System.Globalization;
using LunarFramework.Utility;
using MapPreview.Patches;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewSettings : ModSettings
{
    private const float DefaultPreviewWindowSize = 250f;

    private static Vector2 _scrollPos = Vector2.zero;

    public float PreviewWindowSize = DefaultPreviewWindowSize;
    public bool EnableTrueTerrainColors = true;
    public bool EnableMapPreview = true;
    public bool EnableToolbar = true;
    public bool LockWindowPositions;
    public bool EnableSeedRerollFeature;
    public bool SkipRiverFlowCalc = true;
    
    public Vector2 PreviewWindowPos = new(-1, -1);
    public Vector2 ToolbarWindowPos = new(-1, -1);

    public void DoSettingsWindowContents(Rect inRect)
    {
        if (!MapPreviewMod.LunarAPI.IsInitialized())
        {
            DoLoadFailMessage(inRect);
            return;
        }
        
        Rect rect = new(0.0f, 0.0f, inRect.width, 300f);
        rect.xMax *= 0.95f;
        
        Listing_Standard listingStandard = new();
        listingStandard.Begin(rect);
        GUI.EndGroup();
        Widgets.BeginScrollView(inRect, ref _scrollPos, rect);
        
        listingStandard.CheckboxLabeled("MapPreview.Settings.EnableMapPreview".Translate(), ref EnableMapPreview, "MapPreview.Settings.EnableMapPreview".Translate());
        
        listingStandard.Gap();
        
        listingStandard.CheckboxLabeled("MapPreview.Settings.EnableToolbar".Translate(), ref EnableToolbar, "MapPreview.Settings.EnableToolbar".Translate());
        
        listingStandard.Gap();

        GUI.enabled = EnableToolbar;
        if (!EnableToolbar) EnableSeedRerollFeature = false;
        listingStandard.CheckboxLabeled("MapPreview.Settings.EnableSeedRerollFeature".Translate(), ref EnableSeedRerollFeature, "MapPreview.Settings.EnableSeedRerollFeature".Translate());
        GUI.enabled = true;
        
        listingStandard.Gap();

        listingStandard.CheckboxLabeled("MapPreview.Settings.EnableTrueTerrainColors".Translate(), ref EnableTrueTerrainColors, "MapPreview.Settings.EnableTrueTerrainColors".Translate());
        
        listingStandard.Gap();
        
        listingStandard.CheckboxLabeled("MapPreview.Settings.SkipRiverFlowCalc".Translate(), ref SkipRiverFlowCalc, "MapPreview.Settings.SkipRiverFlowCalc".Translate());
        
        listingStandard.Gap();
        
        bool prevChanged = GUI.changed;
        GUI.changed = false;
        
        listingStandard.CheckboxLabeled("MapPreview.Settings.LockWindowPositions".Translate(), ref LockWindowPositions, "MapPreview.Settings.LockWindowPositions".Translate());

        listingStandard.Gap();

        if (GUI.changed)
        {
            var previewWindow = MapPreviewWindow.Instance;
            if (previewWindow != null) previewWindow.draggable = !LockWindowPositions;
            var toolbarWindow = MapPreviewToolbar.Instance;
            if (toolbarWindow != null) toolbarWindow.draggable = !LockWindowPositions;
        }

        GUI.changed = false;

        CenteredLabel(listingStandard, "MapPreview.Settings.PreviewWindowSize".Translate(), PreviewWindowSize.ToString(CultureInfo.InvariantCulture));
        PreviewWindowSize = (int) listingStandard.Slider(PreviewWindowSize, 100f, 800f);
        
        if (GUI.changed)
        {
            PreviewWindowPos = new Vector2(-1, -1);
            ToolbarWindowPos = new Vector2(-1, -1);
                
            var previewWindow = MapPreviewWindow.Instance;
            if (previewWindow != null)
            {
                previewWindow.windowRect.position = previewWindow.DefaultPos;
                previewWindow.windowRect.size = previewWindow.InitialSize;
            }
            
            var toolbarWindow = MapPreviewToolbar.Instance;
            if (toolbarWindow != null)
            {
                toolbarWindow.windowRect.position = toolbarWindow.DefaultPos;
                toolbarWindow.windowRect.size = toolbarWindow.InitialSize;
            }
        }
        
        GUI.changed = prevChanged;

        if (Prefs.DevMode && listingStandard.ButtonText("[DEV] Clear cache and recalculate all terrain colors"))
        {
            TrueTerrainColors.CalculateTrueTerrainColors(true);
        }

        Widgets.EndScrollView();
        
        if (GUI.changed) Patch_RimWorld_WorldInterface.Refresh();
    }

    private void DoLoadFailMessage(Rect rect)
    {
        Listing_Standard listingStandard = new();
        listingStandard.Begin(rect);
        listingStandard.Label("An error occured whie loading this mod. Check the log file for more information.");
        listingStandard.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PreviewWindowSize, "PreviewWindowSize", DefaultPreviewWindowSize);
        Scribe_Values.Look(ref EnableTrueTerrainColors, "EnableTrueTerrainColors", true);
        Scribe_Values.Look(ref EnableMapPreview, "EnableMapPreview", true);
        Scribe_Values.Look(ref EnableToolbar, "EnableToolbar", true);
        Scribe_Values.Look(ref LockWindowPositions, "LockWindowPositions");
        Scribe_Values.Look(ref EnableSeedRerollFeature, "EnableSeedRerollFeature");
        Scribe_Values.Look(ref SkipRiverFlowCalc, "SkipRiverFlowCalc", true);
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
        SkipRiverFlowCalc = true;
        PreviewWindowPos = new Vector2(-1, -1);
        ToolbarWindowPos = new Vector2(-1, -1);
    }
    
    private static void CenteredLabel(Listing_Standard listingStandard, string left, string center)
    {
        var labelSize = Text.CalcSize(center);
        var rect = listingStandard.GetRect(28f);
        var centerRect = rect.RightHalf();
        centerRect.xMin -= labelSize.x * 0.5f;
        Widgets.Label(centerRect, center);
        Widgets.Label(rect, left);
    }
}