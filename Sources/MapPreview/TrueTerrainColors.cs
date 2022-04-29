using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MapPreview;

[StaticConstructorOnStartup]
public class TrueTerrainColors
{
    private static Dictionary<string, Color> _defaultMapRerollColors;
    private static Dictionary<string, Color> _trueTerrainColors;

    private static bool _trueTerrainColorsApplied;

    static TrueTerrainColors()
    {
        CalculateTrueTerrainColors();
    }
    
    public static void UpdateTerrainColorsIfNeeded(Dictionary<string, Color> terrainColors)
    {
        _defaultMapRerollColors ??= new Dictionary<string, Color>(terrainColors);

        if (ModInstance.Settings.EnableTrueTerrainColors != _trueTerrainColorsApplied)
        {
            if (ModInstance.Settings.EnableTrueTerrainColors && _trueTerrainColors != null)
            {
                terrainColors.Clear();
                terrainColors.AddRange(_trueTerrainColors);
                _trueTerrainColorsApplied = true;
            }
            else
            {
                terrainColors.Clear();
                terrainColors.AddRange(_defaultMapRerollColors);
                _trueTerrainColorsApplied = false;
            }
        }
    }

    public static void CalculateTrueTerrainColors()
    {
        Dictionary<string, Color> trueTerrainColors = new();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var def in DefDatabase<TerrainDef>.AllDefsListForReading)
        {
            try
            {
                var texture = def.graphic.MatSingle.mainTexture;
                if (texture is Texture2D texture2D)
                {
                    var readableTex = DuplicateTexture(texture2D);
                    var rawColor = AverageColorFromTexture(readableTex);
                    var combinedColor = rawColor * def.graphic.MatSingle.color * def.graphic.color;
                    trueTerrainColors.Add(def.defName, combinedColor);
                    Object.Destroy(readableTex);
                }
            }
            catch (Exception e)
            {
                Log.Message($"Failed to extract true color from terrain {def.defName}.");
                Debug.LogException(e);
            }
        }
        
        stopwatch.Stop();
        var count = DefDatabase<TerrainDef>.AllDefsListForReading.Count;
        var time = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
        Log.Message($"[Map Preview] Extracted true colors from {count} terrain defs in {time} s.");

        _trueTerrainColors = trueTerrainColors;
    }
    
    private static Color32 AverageColorFromTexture(Texture2D tex, int stepSize = 1)
    {
        Color32[] texColors = tex.GetPixels32();
 
        int total = texColors.Length;
 
        float r = 0;
        float g = 0;
        float b = 0;
 
        for(int i = 0; i < total; i += stepSize)
        {
            r += texColors[i].r;
            g += texColors[i].g;
            b += texColors[i].b;
        }
 
        return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), 0);
    }
    
    private static Texture2D DuplicateTexture(Texture2D source)
    {
        var renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        var previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        var readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }
}