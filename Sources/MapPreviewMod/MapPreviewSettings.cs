using System.Globalization;
using LunarFramework.Utility;
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
    public bool SkipRiverFlowCalc = true;
    
    public Vector2 PreviewWindowPosition = new(-1, -1);

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

        listingStandard.CheckboxLabeled("MapPreview.Settings.EnableTrueTerrainColors".Translate(), ref EnableTrueTerrainColors, "MapPreview.Settings.EnableTrueTerrainColors".Translate());
        
        listingStandard.Gap();
        
        listingStandard.CheckboxLabeled("MapPreview.Settings.SkipRiverFlowCalc".Translate(), ref SkipRiverFlowCalc, "MapPreview.Settings.SkipRiverFlowCalc".Translate());
        
        listingStandard.Gap();
        
        CenteredLabel(listingStandard, "MapPreview.Settings.PreviewWindowSize".Translate(), PreviewWindowSize.ToString(CultureInfo.InvariantCulture));
        PreviewWindowSize = (int) listingStandard.Slider(PreviewWindowSize, 100f, 800f);

        if (Prefs.DevMode && listingStandard.ButtonText("[DEV] Clear cache and recalculate all terrain colors"))
        {
            TrueTerrainColors.CalculateTrueTerrainColors(true);
        }

        Widgets.EndScrollView();
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
        Scribe_Values.Look(ref SkipRiverFlowCalc, "SkipRiverFlowCalc", true);
        Scribe_Values.Look(ref PreviewWindowPosition, "PreviewWindowPosition", new Vector2(-1, -1));
        base.ExposeData();
    }

    public void ResetAll()
    {
        PreviewWindowSize = DefaultPreviewWindowSize;
        EnableTrueTerrainColors = true;
        EnableMapPreview = true;
        SkipRiverFlowCalc = true;
        PreviewWindowPosition = new Vector2(-1, -1);
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