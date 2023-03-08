using LunarFramework.GUI;
using LunarFramework.Utility;
using MapPreview.Compatibility;
using MapPreview.Patches;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewSettings : LunarModSettings
{
    public readonly Entry<bool> IncludeCaves = MakeEntry(true);
    public readonly Entry<bool> EnableTrueTerrainColors = MakeEntry(true);
    public readonly Entry<bool> EnableMapPreview = MakeEntry(true);
    public readonly Entry<bool> EnableToolbar = MakeEntry(true);
    public readonly Entry<bool> LockWindowPositions = MakeEntry(false);
    public readonly Entry<bool> AutoOpenPreviewOnWorldMap = MakeEntry(false);
    public readonly Entry<bool> EnableSeedRerollFeature = MakeEntry(false);
    public readonly Entry<bool> EnableWorldSeedRerollFeature = MakeEntry(true);
    public readonly Entry<bool> SkipRiverFlowCalc = MakeEntry(true);
    public readonly Entry<bool> EnableMapDesignerIntegration = MakeEntry(true);
    public readonly Entry<bool> EnablePrepareLandingIntegration = MakeEntry(true);
    public readonly Entry<bool> EnableWorldEditIntegration = MakeEntry(true);
    public readonly Entry<float> PreviewWindowSize = MakeEntry(250f);
    public readonly Entry<Vector2> PreviewWindowPos = MakeEntry(new Vector2(-1, -1));
    public readonly Entry<Vector2> ToolbarWindowPos = MakeEntry(new Vector2(-1, -1));

    protected override string TranslationKeyPrefix => "MapPreview.Settings";

    public MapPreviewSettings() : base(MapPreviewMod.LunarAPI)
    {
        MakeTab("Tab.Preview", DoPreviewSettingsTab);
        MakeTab("Tab.Toolbar", DoToolbarSettingsTab);
    }

    public void DoPreviewSettingsTab(LayoutRect layout)
    {
        layout.PushChanged();

        LunarGUI.Checkbox(layout, ref EnableMapPreview.Value, Label("EnableMapPreview"));

        layout.Abs(10f);
        layout.PushEnabled(EnableMapPreview);

        LunarGUI.Checkbox(layout, ref IncludeCaves.Value, Label("IncludeCaves"));
        LunarGUI.Checkbox(layout, ref SkipRiverFlowCalc.Value, Label("SkipRiverFlowCalc"));
        LunarGUI.Checkbox(layout, ref EnableTrueTerrainColors.Value, Label("EnableTrueTerrainColors"));

        if (layout.PopChanged())
        {
            Patch_RimWorld_WorldInterface.Refresh(true);
        }

        layout.Abs(10f);
        layout.PushChanged();

        LunarGUI.Checkbox(layout, ref AutoOpenPreviewOnWorldMap.Value, Label("AutoOpenPreviewOnWorldMap"));

        if (layout.PopChanged() && AutoOpenPreviewOnWorldMap) Patch_RimWorld_WorldInterface.Refresh(true);

        layout.PushChanged();

        LunarGUI.Checkbox(layout, ref LockWindowPositions.Value, Label("LockWindowPositions"));

        if (layout.PopChanged())
        {
            var previewWindow = MapPreviewWindow.Instance;
            if (previewWindow != null) previewWindow.draggable = !LockWindowPositions;
            var toolbarWindow = MapPreviewToolbar.Instance;
            if (toolbarWindow != null) toolbarWindow.draggable = !LockWindowPositions;
        }

        layout.Abs(10f);
        layout.PushChanged();

        LunarGUI.LabelDouble(layout, Label("PreviewWindowSize"), PreviewWindowSize.Value.ToString("F0"));
        LunarGUI.Slider(layout, ref PreviewWindowSize.Value, 100f, 800f);

        if (layout.PopChanged())
        {
            PreviewWindowPos.Value = new Vector2(-1, -1);
            ToolbarWindowPos.Value = new Vector2(-1, -1);

            MapPreviewWindow.Instance?.ResetPositionAndSize();
            MapPreviewToolbar.Instance?.ResetPositionAndSize();
        }

        layout.PopEnabled();
        layout.Abs(10f);

        if (Prefs.DevMode && LunarGUI.Button(layout, "[DEV] Clear cache and recalculate all terrain colors"))
        {
            TrueTerrainColors.CalculateTrueTerrainColors(true);
            Patch_RimWorld_WorldInterface.Refresh();
        }
    }

    public void DoToolbarSettingsTab(LayoutRect layout)
    {
        layout.PushChanged();

        LunarGUI.Checkbox(layout, ref EnableToolbar.Value, Label("EnableToolbar"));

        if (layout.PopChanged())
        {
            Patch_RimWorld_WorldInterface.Refresh();
        }

        layout.Abs(10f);

        layout.PushEnabled(EnableToolbar && !ModCompat_MapReroll.IsPresent);

        var trEntry = ModCompat_MapReroll.IsPresent ? "EnableSeedRerollFeature.MapRerollConflict" : "EnableSeedRerollFeature";
        LunarGUI.Checkbox(layout, ref EnableSeedRerollFeature.Value, Label(trEntry));

        layout.PopEnabled();
        layout.PushEnabled(EnableToolbar);

        LunarGUI.Checkbox(layout, ref EnableWorldSeedRerollFeature.Value, Label("EnableWorldSeedRerollFeature"));

        if (ModCompat_MapDesigner.IsPresent)
        {
            LunarGUI.Checkbox(layout, ref EnableMapDesignerIntegration.Value, "MapPreview.Integration.MapDesigner.Enabled".Translate());
        }

        if (ModCompat_PrepareLanding.IsPresent)
        {
            LunarGUI.Checkbox(layout, ref EnablePrepareLandingIntegration.Value, "MapPreview.Integration.PrepareLanding.Enabled".Translate());
        }

        if (ModCompat_WorldEdit.IsPresent)
        {
            LunarGUI.Checkbox(layout, ref EnableWorldEditIntegration.Value, "MapPreview.Integration.WorldEdit.Enabled".Translate());
        }

        layout.PopEnabled();
    }
}
