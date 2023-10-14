using System;
using System.Collections.Generic;
using LunarFramework.GUI;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public class MapSeedRerollWindow : Window
{
    public static MapSeedRerollWindow Instance => Find.WindowStack?.WindowOfType<MapSeedRerollWindow>();

    private static readonly PatchGroupSubscriber PatchGroupSubscriber = new(typeof(MapSeedRerollWindow));

    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

    protected override float Margin => 0f;

    private readonly LayoutRect _layout = new(MapPreviewMod.LunarAPI);

    private readonly List<Element> _elements = new();

    private int _tileId;
    private int _actSeed;
    private int _lastSeed;
    private IntVec2 _mapSize;
    private MapParent _mapParent;
    private SeedRerollData _data;

    private static Vector2 _elementSize;
    private static Vector2Int _gridSize;

    private int DesiredCount => _gridSize.x * _gridSize.y;

    private const float ElementSpacing = 20f;
    private const float WindowMargin = 20f;

    public MapSeedRerollWindow()
    {
        layer = WindowLayer.SubSuper;
        forcePause = true;
        resizeable = false;
        draggable = false;
    }

    public void UpdateElementSize(int elementsPerRow)
    {
        var width = (windowRect.width - WindowMargin * 2f - (elementsPerRow - 1) * ElementSpacing) / elementsPerRow;
        _elementSize = new Vector2(width, width / _mapSize.x * _mapSize.z);
        var maxRows = (int) Mathf.Floor((windowRect.height - WindowMargin * 2f - 70f) / (_elementSize.y + ElementSpacing));
        _gridSize = new Vector2Int(elementsPerRow, maxRows);

        while (_elements.Count > DesiredCount)
        {
            _elements[_elements.Count - 1].Dispose();
            _elements.RemoveAt(_elements.Count - 1);
        }

        if (!MapPreviewAPI.IsGeneratingPreview) TryAddElement();
    }

    public override void PreOpen()
    {
        base.PreOpen();

        if (Instance != this) Instance?.Close();

        var world = Find.World;

        _tileId = MapPreviewWindow.CurrentTile;
        _data = world.GetComponent<SeedRerollData>();
        _mapParent = world.worldObjects.MapParentAt(_tileId);

        if (world == null || _data == null || _tileId < 0) throw new Exception("Reroll helper window is missing context");

        MapPreviewAPI.SubscribeGenPatches(PatchGroupSubscriber);

        MapPreviewGenerator.Init();
        MapPreviewGenerator.Instance.ClearQueue();

        _mapSize = MapPreviewWindow.DetermineMapSize(world, _tileId);
        _actSeed = _data.TryGet(_tileId, out var savedSeed) ? savedSeed : SeedRerollData.GetOriginalMapSeed(world, _tileId);

        _lastSeed = _actSeed;
        unchecked { _lastSeed += 1; }

        if (DesiredCount <= 0) UpdateElementSize(6);
        else TryAddElement();
    }

    public override void PreClose()
    {
        base.PreClose();

        if (_data != null)
        {
            if (_actSeed != SeedRerollData.GetOriginalMapSeed(_data.world, _tileId))
            {
                _data.Commit(_tileId, _actSeed);
            }
            else
            {
                _data.Reset(_tileId);
            }
        }

        MapPreviewGenerator.Instance.ClearQueue();
        foreach (var element in _elements) element.Dispose();

        MapPreviewAPI.UnsubscribeGenPatches(PatchGroupSubscriber);
    }

    private void TryAddElement()
    {
        if (_elements.Count >= DesiredCount) return;

        var element = new Element(_mapSize, _lastSeed);
        _elements.Add(element);

        var request = new MapPreviewRequest(_lastSeed, _tileId, _mapSize)
        {
            TextureSize = new IntVec2(element.Texture.width, element.Texture.height),
            GeneratorDef = _mapParent?.MapGeneratorDef ?? MapGeneratorDefOf.Base_Player,
            UseTrueTerrainColors = MapPreviewMod.Settings.EnableTrueTerrainColors,
            SkipRiverFlowCalc = MapPreviewMod.Settings.SkipRiverFlowCalc,
            UseMinimalMapComponents = MapPreviewMod.Settings.ExperimentalOptimizations,
            ExistingBuffer = element.Buffer
        };

        if (MapPreviewMod.Settings.IncludeCaves)
        {
            request.GenStepFilter = s => MapPreviewRequest.DefaultGenStepFilter(s) || s.defName == "Caves";
        }

        _data.Commit(_tileId, _lastSeed, false);

        MapPreviewGenerator.Instance.QueuePreviewRequest(request);
        element.Await(request);

        unchecked { _lastSeed += 1; }

        request.Promise.Then(_ =>
        {
            if (IsOpen) TryAddElement();
        });
    }

    private void RefreshSeeds()
    {
        _elements.RemoveAll(e =>
        {
            if (e.Pinned) return false;
            e.Dispose();
            return true;
        });
        
        TryAddElement();
    }

    private static readonly Texture2D _pinIcon = ContentFinder<Texture2D>.Get("UI/Icons/Pin-Outline");
    private static readonly Texture2D _unpinIcon = ContentFinder<Texture2D>.Get("UI/Icons/Pin");
    private static readonly Texture2D _confirmIcon = ContentFinder<Texture2D>.Get("ConfirmMP");
    private static readonly Texture2D _refreshSeedsIcon = ContentFinder<Texture2D>.Get("RerollWorldSeedMP");

    public override void DoWindowContents(Rect inRect)
    {
        bool DoButton(Texture icon, string tooltip, bool enabled = true)
        {
            var buttonPos = _layout.Abs();
            TooltipHandler.TipRegion(buttonPos, tooltip);
            _layout.PushEnabled(enabled);
            var pressed = GUI.Button(buttonPos, icon);
            _layout.PopEnabled();
            return pressed;
        }

        _layout.BeginRoot(inRect, new LayoutParams { Margin = new(WindowMargin) });

        _layout.BeginAbs(50f, new LayoutParams { Horizontal = true });

        GUI.color = new ColorInt(135, 135, 135).ToColor;
        Widgets.DrawBox(_layout);
        GUI.color = Color.white;

        _layout.BeginRel(0.5f, new LayoutParams { Margin = new(14, 10, 18, 10), Spacing = 10, Horizontal = true });
        LunarGUI.Label(_layout, "MapPreview.World.RerollWindow.Title".Translate());
        _layout.End();

        _layout.BeginRel(0.5f, new LayoutParams { Margin = new(10), Spacing = 10, DefaultSize = 30, Horizontal = true, Reversed = true });

        var gridFull = _elements.Count >= DesiredCount && !MapPreviewAPI.IsGeneratingPreview;

        if (DoButton(TexButton.DeleteX, "MapPreview.World.RerollWindow.Close".Translate()))
        {
            Close();
        }

        if (DoButton(_refreshSeedsIcon, "MapPreview.World.RerollWindow.RefreshSeeds".Translate(), gridFull))
        {
            RefreshSeeds();
        }

        if (DoButton(TexButton.Minus, "MapPreview.World.RerollWindow.ZoomOut".Translate(), _gridSize.x < 10))
        {
            UpdateElementSize(_gridSize.x + 1);
        }

        if (DoButton(TexButton.Plus, "MapPreview.World.RerollWindow.ZoomIn".Translate(), _gridSize.x > 3))
        {
            UpdateElementSize(_gridSize.x - 1);
        }

        _layout.End();
        _layout.End();

        _layout.Abs(WindowMargin);

        var idx = 0;

        for (int y = 0; y < _gridSize.y; y++)
        {
            if (idx >= _elements.Count) break;

            _layout.BeginAbs(_elementSize.y, new LayoutParams { Spacing = ElementSpacing, Horizontal = true });

            for (int x = 0; x < _gridSize.x; x++)
            {
                if (idx >= _elements.Count) break;

                var element = _elements[idx];

                _layout.BeginAbs(_elementSize.x, new LayoutParams { Horizontal = true, Reversed = true });

                element.Draw(_layout);

                var hovered = Mouse.IsOver(_layout);

                if (hovered) Widgets.DrawHighlight(_layout);

                _layout.BeginAbs(50f, new LayoutParams { Margin = new(10), Spacing = 10 });

                if (hovered || element.Pinned)
                {
                    var btnRect = _layout.Abs(30);
                    TooltipHandler.TipRegion(btnRect, ("MapPreview.World.RerollWindow." + (element.Pinned ? "Unpin" : "Pin")).Translate());
                    if (Widgets.ButtonImage(btnRect, element.Pinned ? _unpinIcon : _pinIcon))
                    {
                        element.Pinned = !element.Pinned;
                    }
                }

                if (hovered)
                {
                    var btnRect = _layout.Abs(30);
                    TooltipHandler.TipRegion(btnRect, "MapPreview.World.RerollWindow.Apply".Translate());
                    if (Widgets.ButtonImage(btnRect, _confirmIcon))
                    {
                        _actSeed = element.Seed;
                        Close();
                    }
                }

                _layout.End();
                _layout.End();

                idx++;
            }

            _layout.End();
            _layout.Abs(ElementSpacing);
        }

        _layout.End();
    }

    private class Element : MapPreviewWidgetWithPreloader
    {
        public readonly int Seed;

        public bool Pinned;

        public Element(IntVec2 maxMapSize, int seed) : base(maxMapSize)
        {
            Seed = seed;
        }

        protected override void DrawOutline(Rect rect) { }
    }
}
