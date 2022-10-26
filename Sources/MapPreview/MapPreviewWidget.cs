/*
 
Modified version of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/UI/Widget_MapPreview.cs

MIT License

Copyright (c) 2017 UnlimitedHugs, modifications (c) 2022 m00nl1ght <https://github.com/m00nl1ght-dev>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

using System;
using System.Collections.Generic;
using MapPreview.Interpolation;
using MapPreview.Promises;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace MapPreview;

public abstract class MapPreviewWidget : IDisposable
{
    protected static readonly Color DefaultOutlineColor = GenColor.FromHex("616C7A");
    
    protected virtual float SpawnInterpolationDuration => 0.3f;
    protected virtual Color OutlineColor => DefaultOutlineColor;

    protected readonly ValueInterpolator SpawnInterpolator;

    protected Rect TexCoords;
    protected int AwaitingMapTile = -1;
    protected MapPreviewRequest AwaitingRequest;
    
    public Color[] Buffer { get; private set; }
    public Texture2D Texture { get; private set; }
    public Map PreviewMap { get; private set; }

    protected readonly List<MapPreviewOverlay> Overlays = new();

    protected MapPreviewWidget(IntVec2 maxMapSize)
    {
        SpawnInterpolator = new ValueInterpolator();
        Texture = new Texture2D(maxMapSize.x, maxMapSize.z, TextureFormat.RGB24, false);
    }

    public void AddOverlay(MapPreviewOverlay overlay)
    {
        if (overlay.PreviewWidget != this) throw new Exception();
        Overlays.Add(overlay);
    }
    
    public void Await(MapPreviewRequest request)
    {
        AwaitingRequest = request;
        Await(request.Promise, request.MapTile);
    }

    public void Await(IPromise<MapPreviewResult> promise, int mapTile = -1)
    {
        SpawnInterpolator.finished = true;
        SpawnInterpolator.value = 0f;
        AwaitingMapTile = mapTile;
        
        promise.Done(OnPromiseResolved, OnPromiseRejected);
    }

    public void Dispose()
    {
        Object.Destroy(Texture);
        AwaitingMapTile = -1;
        AwaitingRequest = null;
        PreviewMap = null;
        Texture = null;
    }

    public void Draw(Rect rect)
    {
        if (Event.current.type == EventType.Repaint)
        {
            SpawnInterpolator.Update();
            if (SpawnInterpolator.value < 1)
            {
                DrawGenerating(rect);
            }
        }

        DrawOutline(rect);
        
        if (Texture != null && SpawnInterpolator.value > 0)
        {
            DrawGenerated(rect);
            
            foreach (var overlay in Overlays)
            {
                overlay.Draw(rect);
            }

            if (PreviewMap != null && Event.current.type == EventType.Repaint)
            {
                var pos = Event.current.mousePosition - rect.position;
                TooltipHandler.TipRegion(rect, new TipSignal(() => MakeTooltip(PreviewMap, MapPosFromScreenPos(rect, pos)), GetHashCode()));
            }
        }
    }

    public IntVec3 MapPosFromScreenPos(Rect mapRect, Vector2 screenPos)
    {
        double rx = PreviewMap.Size.x / mapRect.width;
        double rz = PreviewMap.Size.z / mapRect.height;
        double x = screenPos.x * rx, z = PreviewMap.Size.z - screenPos.y * rz;
        int ix = Math.Min(PreviewMap.Size.x - 1, Math.Max(0, (int) Math.Round(x, 0)));
        int iz = Math.Min(PreviewMap.Size.z - 1, Math.Max(0, (int) Math.Round(z, 0)));
        return new IntVec3(ix, 0, iz);
    }
    
    public Vector2 PosInRectFromMapPos(Rect mapRect, IntVec3 mapPos)
    {
        float rx = PreviewMap.Size.x / mapRect.width;
        float rz = PreviewMap.Size.z / mapRect.height;
        float x = mapPos.x / rx, z = PreviewMap.Size.z - mapPos.y / rz;
        float fx = Math.Min(mapRect.width, Math.Max(0, x));
        float fz = Math.Min(mapRect.height, Math.Max(0, z));
        return new Vector2(fx, fz);
    }

    protected virtual string MakeTooltip(Map map, IntVec3 pos)
    {
        return map.terrainGrid.TerrainAt(pos).label.CapitalizeFirst() + " ( " + pos.x + " | " + pos.z + " )";
    }

    protected virtual void DrawGenerating(Rect inRect) {}

    protected virtual void DrawGenerated(Rect inRect)
    {
        var texRect = inRect.ScaledBy(SpawnInterpolator.value).ContractedBy(1f);
        GUI.DrawTextureWithTexCoords(texRect, Texture, TexCoords);
    }

    private void OnPromiseResolved(MapPreviewResult result)
    {
        if (Texture == null || result == null || AwaitingMapTile != result.MapTile) return;
        if (AwaitingRequest != null && AwaitingRequest != result.Request) return;

        PreviewMap = result.Map;
        TexCoords = result.TexCoords;
        result.CopyToTexture(Texture);
        Texture.Apply();
        
        AwaitingMapTile = -1;
        AwaitingRequest = null;
        Buffer = result.Pixels;
        
        foreach (var overlay in Overlays)
        {
            overlay.Update(result);
        }
        
        SpawnInterpolator.value = 0f;
        SpawnInterpolator.StartInterpolation(1f, SpawnInterpolationDuration, CurveType.CubicOut);
    }

    private void OnPromiseRejected(Exception ex)
    {
        if (Texture == null) return;

        PreviewMap = null;
        
        foreach (var overlay in Overlays)
        {
            overlay.Reset();
        }
        
        SpawnInterpolator.value = 0f;
        SpawnInterpolator.finished = true;
        
        HandleError(ex);
    }

    protected virtual void HandleError(Exception ex) {}

    private void DrawOutline(Rect rect)
    {
        var oldColor = GUI.color;
        GUI.color = OutlineColor;
        Widgets.DrawBox(rect);
        GUI.color = oldColor;
    }

    public static void DrawPreloader(Texture2D tex, Vector2 center, int offset = 0)
    {
        if (tex == null) return;
        var waveBase = Mathf.Abs(Time.time - offset / 2f);
        var wave = Mathf.Sin((Time.time - offset / 6f) * 3f);
        var texAlpha = 1f - (1 + wave) * .4f;
        const float texScale = 1f;
        var rect = new Rect(center.x - (tex.width / 2f) * texScale, center.y - (tex.height / 2f) * texScale,
            tex.width * texScale, tex.height * texScale);
        var prevColor = GUI.color;
        var baseColor = Color.HSVToRGB((waveBase / 16f) % 1f, 1f, 1f);
        GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, texAlpha);
        GUI.DrawTexture(rect, tex);
        GUI.color = prevColor;
    }
}