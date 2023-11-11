using System;
using System.Collections.Generic;
using System.Linq;
using LunarFramework.GUI;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

public class MapPreviewToolbar : Window
{
    public static MapPreviewToolbar Instance => Find.WindowStack?.WindowOfType<MapPreviewToolbar>();

    public static bool CurrentTileCanBeRerolled { get; private set; }
    public static bool CurrentTileIsRerolled { get; private set; }

    private static readonly List<Button> RegisteredButtons = new();

    public static float MinWidth => 10f + RegisteredButtons.Count(b => b.IsVisible) * 40f;

    public static void RegisterButton(Button button)
    {
        RegisteredButtons.AddDistinct(button);
    }

    static MapPreviewToolbar()
    {
        RegisterButton(new ButtonRerollMap());
        RegisterButton(new ButtonRerollMapUndo());
        RegisterButton(new ButtonRerollWorld());
        RegisterButton(new ButtonOpenSettings());
    }

    public Vector2 DefaultPos => new(UI.screenWidth - Mathf.Max(MapPreviewMod.Settings.PreviewWindowSize, MinWidth) - 50f, 50f);
    public override Vector2 InitialSize => new(Math.Max(MapPreviewMod.Settings.PreviewWindowSize, MinWidth), 50);

    protected override float Margin => 0f;

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
        draggable = !MapPreviewMod.Settings.LockWindowPositions;
    }

    public void OnWorldTileSelected(World world, int tileId, MapParent mapParent)
    {
        if (tileId >= 0 && WorldInterfaceManager.ShouldPreviewForTile(world.grid[tileId], tileId, mapParent))
        {
            var rerollData = world.GetComponent<SeedRerollData>();
            if (rerollData == null)
            {
                MapPreviewMod.Logger.Warn("This world is missing the SeedRerollData component, adding it.");
                rerollData = new SeedRerollData(world);
                world.components.Add(rerollData);
            }
            
            CurrentTileIsRerolled = rerollData.TryGet(tileId, out _);
            CurrentTileCanBeRerolled = mapParent is not { HasMap: true };
        }
        else
        {
            CurrentTileIsRerolled = false;
            CurrentTileCanBeRerolled = false;
        }
    }

    public void ResetPositionAndSize()
    {
        windowRect.position = DefaultPos;
        windowRect.size = InitialSize;
    }

    public override void PreOpen()
    {
        base.PreOpen();

        if (Instance != this) Instance?.Close();

        CurrentTileCanBeRerolled = false;
        CurrentTileIsRerolled = false;

        Vector2 pos = MapPreviewMod.Settings.ToolbarWindowPos;

        windowRect.x = pos.x >= 0 ? pos.x : DefaultPos.x;
        windowRect.y = pos.y >= 0 ? pos.y : DefaultPos.y;

        if (windowRect.x + windowRect.width > UI.screenWidth) windowRect.x = DefaultPos.x;
        if (windowRect.y + windowRect.height > UI.screenHeight) windowRect.y = DefaultPos.y;
    }

    public override void PreClose()
    {
        base.PreClose();

        var pos = new Vector2((int) windowRect.x, (int) windowRect.y);
        if (pos != MapPreviewMod.Settings.ToolbarWindowPos)
        {
            MapPreviewMod.Settings.ToolbarWindowPos.Value = pos;
            MapPreviewMod.Settings.Write();
        }
    }

    private readonly LayoutRect _layout = new(MapPreviewMod.LunarAPI);

    public override void DoWindowContents(Rect inRect)
    {
        void DoButton(Button button)
        {
            if (button.IsVisible)
            {
                var buttonPos = _layout.Abs();
                TooltipHandler.TipRegion(buttonPos, button.Tooltip);
                GUI.enabled = button.IsInteractable;
                if (GUI.Button(buttonPos, button.Icon)) button.OnAction();
            }
        }

        _layout.BeginRoot(inRect, new LayoutParams { Margin = new(10), Horizontal = true });

        _layout.BeginRel(0.5f, new LayoutParams { Spacing = 10, DefaultSize = 30, Horizontal = true });
        foreach (var button in RegisteredButtons.Where(b => !b.AlignRight)) DoButton(button);
        _layout.End();

        _layout.BeginRel(0.5f, new LayoutParams { Spacing = 10, DefaultSize = 30, Horizontal = true, Reversed = true });
        foreach (var button in RegisteredButtons.Where(b => b.AlignRight)) DoButton(button);
        _layout.End();

        _layout.End();

        GUI.enabled = true;

        if (Input.GetMouseButtonUp(0))
        {
            var mpWindow = MapPreviewWindow.Instance;
            if (mpWindow != null)
            {
                var rect = mpWindow.windowRect;
                if (Math.Abs(windowRect.x - rect.x) < 15)
                {
                    windowRect.x = rect.x;
                }
                else if (Math.Abs(windowRect.xMax - rect.xMax) < 15)
                {
                    windowRect.x = rect.xMax - windowRect.width;
                }
            }
        }
    }

    public abstract class Button
    {
        public virtual bool IsVisible => true;
        public virtual bool IsInteractable => true;
        public virtual bool AlignRight => false;

        public abstract string Tooltip { get; }
        public abstract Texture Icon { get; }

        public abstract void OnAction();
    }

    private class ButtonRerollMap : Button
    {
        public override bool IsVisible => MapPreviewMod.Settings.EnableSeedRerollFeature || CurrentTileIsRerolled;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview && CurrentTileCanBeRerolled;

        public override string Tooltip => "MapPreview.World.RerollMapSeed".Translate();
        public override Texture Icon => MapPreviewWidgetWithPreloader.UIPreviewLoading;

        public override void OnAction()
        {
            if (MapPreviewMod.Settings.EnableSeedRerollWindow == Input.GetKey(KeyCode.LeftShift))
            {
                var tile = MapPreviewWindow.CurrentTile;
                var rerollData = Find.World.GetComponent<SeedRerollData>();
                var seed = rerollData.TryGet(tile, out var savedSeed) ? savedSeed : SeedRerollData.GetOriginalMapSeed(Find.World, tile);

                unchecked { seed += 1; }

                rerollData.Commit(tile, seed);
            }
            else
            {
                Find.WindowStack.Add(new MapSeedRerollWindow());
            }
        }
    }

    private class ButtonRerollMapUndo : Button
    {
        public override bool IsVisible => CurrentTileIsRerolled;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview && CurrentTileIsRerolled && CurrentTileCanBeRerolled;

        public override string Tooltip => "MapPreview.World.ResetMapSeed".Translate();
        public override Texture Icon => MapPreviewWidgetWithPreloader.UIPreviewReset;

        public override void OnAction()
        {
            var tile = MapPreviewWindow.CurrentTile;
            var rerollData = Find.World.GetComponent<SeedRerollData>();
            rerollData.Reset(tile);
        }
    }

    private class ButtonRerollWorld : Button
    {
        public override bool IsVisible => Current.ProgramState == ProgramState.Entry && MapPreviewMod.Settings.EnableWorldSeedRerollFeature;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview;

        public override string Tooltip => "MapPreview.World.RerollWorldSeed".Translate();
        public override Texture Icon { get; } = ContentFinder<Texture2D>.Get("RerollWorldSeedMP");

        public override void OnAction()
        {
            var windowStack = Find.WindowStack;
            var page = windowStack.WindowOfType<Page_SelectStartingSite>();

            if (page is { prev: Page_CreateWorldParams paramsPage })
            {
                windowStack.Add(paramsPage);
                page.Close();

                windowStack.WindowOfType<WorldInspectPane>()?.Close();

                MapPreviewWindow.Instance?.Close();
                WorldInterfaceManager.RefreshPreview();
                Instance?.OnWorldTileSelected(null, -1, null);

                paramsPage.seedString = GenText.RandomSeedString();
                paramsPage.CanDoNext();
            }
        }
    }

    private class ButtonOpenSettings : Button
    {
        public override string Tooltip => "MapPreview.World.OpenSettings".Translate();
        public override Texture Icon { get; } = ContentFinder<Texture2D>.Get(OptionCategoryDefOf.General.texPath);

        public override bool AlignRight => true;

        public override void OnAction()
        {
            var windowStack = Find.WindowStack;

            if (Prefs.DevMode && Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKey(KeyCode.S))
                {
                    LunarGUI.OpenGenericWindow(MapPreviewMod.LunarAPI, new Vector2(500, 500), BiomeWorkerScoresDebugGUI);
                }
                else
                {
                    var existing = windowStack.WindowOfType<Dialog_Options>();
                    if (existing == null) windowStack.Add(new Dialog_Options(OptionCategoryDefOf.Mods));
                    else existing.Close();
                }
            }
            else
            {
                var existing = windowStack.WindowOfType<Dialog_ModSettings>();
                if (existing == null) windowStack.Add(new Dialog_ModSettings(MapPreviewMod.Settings.Mod));
                else existing.Close();
            }
        }

        private void BiomeWorkerScoresDebugGUI(Window window, LayoutRect layout)
        {
            LunarGUI.Label(layout, "Biome worker scores for selected tile");
            LunarGUI.SeparatorLine(layout, 3f);

            var tileId = MapPreviewWindow.CurrentTile;
            if (tileId < 0) return;

            var tile = Find.WorldGrid[tileId];
            var dict = DefDatabase<BiomeDef>.AllDefs.ToDictionary(b => b, b => b.Worker.GetScore(tile, tileId));

            foreach (var pair in dict.OrderByDescending(p => p.Value))
            {
                LunarGUI.LabelDouble(layout, pair.Key.LabelCap, pair.Value.ToString("F2"), false);
            }
        }
    }
}
