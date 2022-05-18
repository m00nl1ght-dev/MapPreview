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
using HugsLib;
using MapPreview.Interpolation;
using MapPreview.Promises;
using UnityEngine;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public class MapPreview : IDisposable
{
    private const float SpawnInterpolationDuration = .3f;

    private static readonly Color OutlineColor = GenColor.FromHex("616C7A");

    private static readonly Texture2D UIPreviewLoading = ContentFinder<Texture2D>.Get("UIPreviewLoadingMP");

    private readonly ValueInterpolator _spawnInterpolator;

    private Rect _texCoords;
    private int _awaitingMapTile = -1;

    public Texture2D Texture { get; private set; }

    public MapPreview(int maxMapSize)
    {
        _spawnInterpolator = new ValueInterpolator();
        Texture = new Texture2D(maxMapSize, maxMapSize, TextureFormat.RGB24, false);
    }

    public void Await(IPromise<ExactMapPreviewGenerator.ThreadableTexture> promise, int mapTile)
    {
        _spawnInterpolator.finished = true;
        _spawnInterpolator.value = 0f;
        _awaitingMapTile = mapTile;
        
        promise.Done(OnPromiseResolved, OnPromiseRejected);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(Texture);
        _awaitingMapTile = -1;
        Texture = null;
    }

    public void Draw(Rect inRect, int index)
    {
        if (Event.current.type == EventType.Repaint)
        {
            _spawnInterpolator.Update();
            if (_spawnInterpolator.value < 1)
            {
                DrawPreloader(inRect.center, index);
            }
        }

        DrawOutline(inRect);
        if (Texture != null)
        {
            var texScale = _spawnInterpolator.value;
            if (texScale > 0)
            {
                var texRect = inRect.ScaledBy(texScale).ContractedBy(1f);
                GUI.DrawTextureWithTexCoords(texRect, Texture, _texCoords);
            }
        }
    }

    private void OnPromiseResolved(ExactMapPreviewGenerator.ThreadableTexture result)
    {
        if (Texture == null || result == null || _awaitingMapTile != result.MapTile) return;

        _awaitingMapTile = -1;
        _texCoords = result.TexCoords;
        result.CopyToTexture(Texture);
        Texture.Apply();
        
        _spawnInterpolator.value = 0f;
        _spawnInterpolator.StartInterpolation(1f, SpawnInterpolationDuration, CurveType.CubicOut);
    }

    private void OnPromiseRejected(Exception ex)
    {
        if (Texture == null) return;
        
        _spawnInterpolator.value = 0f;
        _spawnInterpolator.finished = true;
        
        Find.WindowStack.Add(new Dialog_MessageBox(
            "MapPreview.PreviewGenerationFailed".Translate(),
            null, () => { HugsLibController.Instance.LogUploader.ShowPublishPrompt(); }
        ));
    }

    private void DrawOutline(Rect rect)
    {
        var oldColor = GUI.color;
        GUI.color = OutlineColor;
        Widgets.DrawBox(rect);
        GUI.color = oldColor;
    }

    public static void DrawPreloader(Vector2 center, int index)
    {
        var waveBase = Mathf.Abs(Time.time - index / 2f);
        var wave = Mathf.Sin((Time.time - index / 6f) * 3f);
        var tex = UIPreviewLoading;
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