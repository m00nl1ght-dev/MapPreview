using System.Globalization;
using UnityEngine;
using Verse;

namespace MapPreview;

public class Settings : ModSettings
{
    private const float DefaultPreviewWindowSize = 250f;

    private static Vector2 _scrollPos = Vector2.zero;

    public float PreviewWindowSize = DefaultPreviewWindowSize;
    public bool EnableTrueTerrainColors = true;
    public bool EnableExactPreviewGenerator;
    public bool EnableMapPreview = true;
    public bool EnableMapReroll = true;
    
    public Vector2 PreviewWindowPosition = new Vector2(-1, -1);

    public void DoSettingsWindowContents(Rect inRect)
    {
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
        
        CenteredLabel(listingStandard, "MapPreview.Settings.PreviewWindowSize".Translate(), PreviewWindowSize.ToString(CultureInfo.InvariantCulture));
        PreviewWindowSize = (int) listingStandard.Slider(PreviewWindowSize, 100f, 800f);

        if (Prefs.DevMode && listingStandard.ButtonText("[DEV] Clear cache and recalculate all terrain colors"))
        {
            TrueTerrainColors.CalculateTrueTerrainColors(true);
        }

        Widgets.EndScrollView();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref PreviewWindowSize, "PreviewWindowSize", DefaultPreviewWindowSize);
        Scribe_Values.Look(ref EnableTrueTerrainColors, "EnableTrueTerrainColors", true);
        Scribe_Values.Look(ref EnableExactPreviewGenerator, "EnableExactPreviewGenerator_Exp");
        Scribe_Values.Look(ref EnableMapPreview, "EnableMapPreview", true);
        Scribe_Values.Look(ref EnableMapReroll, "EnableMapReroll", true);
        Scribe_Values.Look(ref PreviewWindowPosition, "PreviewWindowPosition", new Vector2(-1, -1));
        base.ExposeData();
    }

    public void ResetAll()
    {
        PreviewWindowSize = DefaultPreviewWindowSize;
        EnableTrueTerrainColors = true;
        EnableExactPreviewGenerator = false;
        EnableMapPreview = true;
        EnableMapReroll = true;
        PreviewWindowPosition = new Vector2(-1, -1);
    }
    
    private static void CenteredLabel(Listing_Standard listingStandard, string left, string center)
    {
        Vector2 labelSize = Text.CalcSize(center);
        Rect rect = listingStandard.GetRect(28f);
        Rect centerRect = rect.RightHalf();
        centerRect.xMin -= labelSize.x * 0.5f;
        Widgets.Label(centerRect, center);
        Widgets.Label(rect, left);
    }
}