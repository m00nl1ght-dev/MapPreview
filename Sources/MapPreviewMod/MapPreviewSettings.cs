using LunarFramework.GUI;
using LunarFramework.Utility;
using MapPreview.Compatibility;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewSettings : LunarModSettings
{
    public readonly Entry<bool> IncludeCaves = MakeEntry(true);
    public readonly Entry<bool> EnableTrueTerrainColors = MakeEntry(true);
    public readonly Entry<bool> EnableMapPreview = MakeEntry(true);
    public readonly Entry<bool> EnableMapPreviewInPlay = MakeEntry(true);
    public readonly Entry<bool> EnableToolbar = MakeEntry(true);
    public readonly Entry<bool> EnableToolbarInPlay = MakeEntry(true);
    public readonly Entry<bool> LockWindowPositions = MakeEntry(false);
    public readonly Entry<bool> AutoOpenPreviewOnWorldMap = MakeEntry(false);
    public readonly Entry<bool> TriggerPreviewOnWorldObjects = MakeEntry(true);
    public readonly Entry<bool> EnableSeedRerollFeature = MakeEntry(false);
    public readonly Entry<bool> EnableSeedRerollWindow = MakeEntry(true);
    public readonly Entry<bool> EnableWorldSeedRerollFeature = MakeEntry(true);
    public readonly Entry<bool> SkipRiverFlowCalc = MakeEntry(true);
    public readonly Entry<bool> ExperimentalOptimizations = MakeEntry(false);
    public readonly Entry<bool> EnableMapDesignerIntegration = MakeEntry(true);
    public readonly Entry<bool> EnablePrepareLandingIntegration = MakeEntry(true);
    public readonly Entry<bool> EnableWorldEditIntegration = MakeEntry(true);
    public readonly Entry<bool> EnableLandformSettingsIntegration = MakeEntry(false);
    public readonly Entry<float> PreviewWindowSize = MakeEntry(250f);
    public readonly Entry<Vector2> PreviewWindowPos = MakeEntry(new Vector2(-1, -1));
    public readonly Entry<Vector2> ToolbarWindowPos = MakeEntry(new Vector2(-1, -1));
    
    public bool PreviewEnabledNow => InEntry ? EnableMapPreview : EnableMapPreviewInPlay;
    public bool ToolbarEnabledNow => InEntry ? EnableToolbar : EnableToolbarInPlay;

    public bool PreviewEnabledEver => EnableMapPreview || EnableMapPreviewInPlay;
    public bool ToolbarEnabledEver => EnableToolbar || EnableToolbarInPlay;

    protected override string TranslationKeyPrefix => "MapPreview.Settings";

    private bool InEntry => Current.ProgramState == ProgramState.Entry;

    public MapPreviewSettings() : base(MapPreviewMod.LunarAPI)
    {
        MakeTab("Tab.Preview", DoPreviewSettingsTab);
        MakeTab("Tab.Toolbar", DoToolbarSettingsTab);
    }

    public void DoPreviewSettingsTab(LayoutRect layout)
    {
        layout.PushChanged();
        
        LunarGUI.Label(layout, Label("EnableMapPreviewHeader"));
        
        LunarGUI.Checkbox(layout, ref EnableMapPreview.Value, "    " + Label("EnableMapPreviewInEntry"));
        LunarGUI.Checkbox(layout, ref EnableMapPreviewInPlay.Value, "    " + Label("EnableMapPreviewInPlay"));
        
        layout.Abs(10f);

        layout.PushEnabled(PreviewEnabledNow || (Current.Game == null && PreviewEnabledEver));
        
        LunarGUI.Checkbox(layout, ref IncludeCaves.Value, Label("IncludeCaves"));
        LunarGUI.Checkbox(layout, ref SkipRiverFlowCalc.Value, Label("SkipRiverFlowCalc"));
        LunarGUI.Checkbox(layout, ref EnableTrueTerrainColors.Value, Label("EnableTrueTerrainColors"));
        LunarGUI.Checkbox(layout, ref ExperimentalOptimizations.Value, Label("ExperimentalOptimizations"));

        layout.Abs(10f);
        
        layout.PushEnabled(EnableMapPreviewInPlay);

        LunarGUI.Checkbox(layout, ref AutoOpenPreviewOnWorldMap.Value, Label("AutoOpenPreviewOnWorldMap"));

        layout.PopEnabled();

        if (layout.PopChanged()) WorldInterfaceManager.RefreshInterface();

        LunarGUI.Checkbox(layout, ref TriggerPreviewOnWorldObjects.Value, Label("TriggerPreviewOnWorldObjects"));

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

        if (LunarGUI.Button(layout, Label("RefreshTerrainColors")))
        {
            TrueTerrainColors.CalculateTrueTerrainColors(true);
            WorldInterfaceManager.RefreshPreview();
        }
    }

    public void DoToolbarSettingsTab(LayoutRect layout)
    {
        layout.PushChanged();
        
        LunarGUI.Label(layout, Label("EnableToolbarHeader"));

        LunarGUI.Checkbox(layout, ref EnableToolbar.Value, "    " + Label("EnableToolbarInEntry"));
        LunarGUI.Checkbox(layout, ref EnableToolbarInPlay.Value, "    " + Label("EnableToolbarInPlay"));
        
        if (layout.PopChanged()) WorldInterfaceManager.RefreshInterface();

        layout.Abs(10f);

        layout.PushEnabled(!ModCompat_MapReroll.IsPresent && (ToolbarEnabledNow || (Current.Game == null && ToolbarEnabledEver)));

        var trEntry = ModCompat_MapReroll.IsPresent ? "EnableSeedRerollFeature.MapRerollConflict" : "EnableSeedRerollFeature";
        LunarGUI.Checkbox(layout, ref EnableSeedRerollFeature.Value, Label(trEntry));

        layout.PopEnabled();
        layout.PushEnabled(ToolbarEnabledNow || (Current.Game == null && ToolbarEnabledEver));

        LunarGUI.Checkbox(layout, ref EnableWorldSeedRerollFeature.Value, Label("EnableWorldSeedRerollFeature"));

        if (ModCompat_GeologicalLandforms.IsPresent)
        {
            LunarGUI.Checkbox(layout, ref EnableLandformSettingsIntegration.Value, "MapPreview.Integration.LandformSettings.Enabled".Translate());
        }

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

        layout.Abs(10f);

        if (EnableSeedRerollFeature)
        {
            LunarGUI.Checkbox(layout, ref EnableSeedRerollWindow.Value, Label("EnableSeedRerollWindow"));
        }

        layout.PopEnabled();
    }
}
