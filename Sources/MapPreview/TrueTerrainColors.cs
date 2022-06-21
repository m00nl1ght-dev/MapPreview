using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using Verse;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MapPreview;

[StaticConstructorOnStartup]
public class TrueTerrainColors
{
    private static string CacheFile => Path.Combine(GenFilePaths.ConfigFolderPath, "TrueTerrainColorsCache.xml");

    private static readonly HashSet<string> ExcludeList = new() { "fluffy.stuffedfloors" };

    private static readonly Dictionary<string, Color> DefaultMapRerollColors = new Dictionary<string, Color> {
        {"Sand", GenColor.FromHex("806F54")},
        {"Soil", GenColor.FromHex("6D5B49")},
        {"MarshyTerrain", GenColor.FromHex("3F412B")},
        {"SoilRich", GenColor.FromHex("42362A")},
        {"Gravel", GenColor.FromHex("6D5B49")},
        {"Mud", GenColor.FromHex("403428")},
        {"Marsh", GenColor.FromHex("363D30")},
        {"MossyTerrain", GenColor.FromHex("6D5B49")},
        {"Ice", GenColor.FromHex("9CA7AC")},
        {"WaterDeep", GenColor.FromHex("3A434D")},
        {"WaterOceanDeep", GenColor.FromHex("3A434D")},
        {"WaterMovingDeep", GenColor.FromHex("3A434D")},
        {"WaterShallow", GenColor.FromHex("434F50")},
        {"WaterOceanShallow", GenColor.FromHex("434F50")},
        {"WaterMovingShallow", GenColor.FromHex("434F50")}
    };

    public static IReadOnlyDictionary<string, Color> CurrentTerrainColors => 
        ModInstance.Settings.EnableTrueTerrainColors ? _trueTerrainColors : DefaultMapRerollColors;

    private static Dictionary<string, Color> _trueTerrainColors;
    private static bool _trueTerrainColorsApplied;

    static TrueTerrainColors()
    {
        Log.ResetMessageCount();
        
        if (File.Exists(CacheFile))
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(List<CacheEntry>));
                using var streamReader = File.OpenText(CacheFile);
                if (xmlSerializer.Deserialize(streamReader) is List<CacheEntry> cacheData)
                {
                    _trueTerrainColors = new Dictionary<string, Color>();
                    foreach (var cacheEntry in cacheData) _trueTerrainColors.Add(cacheEntry.DefName, cacheEntry.Color);
                    Log.Message(ModInstance.LogPrefix + $"Loaded cached true colors for {cacheData.Count} terrain defs from file.");
                }
            }
            catch (Exception e)
            {
                _trueTerrainColors = null;
                Log.Warning(ModInstance.LogPrefix + $" Failed to read TrueTerrainColorsCache from file: " + e);
                Debug.LogException(e);
            }
        }
        
        try
        {
            CalculateTrueTerrainColors();
        }
        catch (Exception e)
        {
            ModInstance.Settings.EnableTrueTerrainColors = false;
            Log.Error(ModInstance.LogPrefix + "Unknown error occured while extracting colors from terrain textures. Disabling true terrain colors feature.");
            Debug.LogException(e);
        }
    }

    public static void UpdateTerrainColorsIfNeeded(Dictionary<string, Color> terrainColors)
    {
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
                terrainColors.AddRange(DefaultMapRerollColors);
                _trueTerrainColorsApplied = false;
            }
        }
    }

    public static void CalculateTrueTerrainColors(bool clear = false)
    {
        int maxW = 0, maxH = 0, count = 0;

        _trueTerrainColors ??= new Dictionary<string, Color>();
        if (clear) _trueTerrainColors.Clear();

        var missingDefs = DefDatabase<TerrainDef>.AllDefsListForReading
            .Where(def => !_trueTerrainColors.ContainsKey(def.defName) && !ExcludeList.Contains(def.modContentPack?.PackageId))
            .ToList();
        
        if (missingDefs.Count <= 0) return;

        foreach (var texture in missingDefs.Select(def => def.graphic?.MatSingle?.mainTexture).OfType<Texture2D>())
        {
            if (texture.width > maxW) maxW = texture.width;
            if (texture.height > maxH) maxH = texture.height;
            count++;
        }
        
        if (count <= 0 || maxW <= 0 || maxH <= 0) return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var renderTex = RenderTexture.GetTemporary(maxW, maxH, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        var readableTex = new Texture2D(maxW, maxH);
        var previous = RenderTexture.active;
        RenderTexture.active = renderTex;

        foreach (var def in missingDefs)
        {
            try
            {
                var texture = def.graphic?.MatSingle?.mainTexture;
                if (texture is Texture2D texture2D)
                {
                    Graphics.Blit(texture2D, renderTex);
                    readableTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                    readableTex.Apply();
                    var rawColor = AverageColorFromTexture(readableTex);
                    var combinedColor = rawColor * def.graphic.MatSingle.color * def.graphic.color;
                    _trueTerrainColors.Add(def.defName, combinedColor);
                }
            }
            catch (Exception e)
            {
                Log.Message(ModInstance.LogPrefix + $"Failed to extract true color from terrain {def.defName}.");
                Debug.LogException(e);
            }
        }
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        Object.Destroy(readableTex);
        
        stopwatch.Stop();
        
        var time = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
        Log.Message(ModInstance.LogPrefix + $"Extracted true colors from {count} terrain defs in {time} seconds using a RenderTexture of size {maxW}x{maxH}.");
        
        try
        {
            var cacheEntries = _trueTerrainColors.Select(p => new CacheEntry { DefName = p.Key, Color = p.Value }).ToList();
            var xmlSerializer = new XmlSerializer(typeof(List<CacheEntry>));
            using var streamWriter = File.CreateText(CacheFile);
            xmlSerializer.Serialize(streamWriter, cacheEntries);
        }
        catch (Exception e)
        {
            Log.Warning(ModInstance.LogPrefix + "Failed to write TrueTerrainColorsCache to file: " + e);
            Debug.LogException(e);
        }
    }
    
    private static Color32 AverageColorFromTexture(Texture2D tex, int stepSize = 1)
    {
        var texColors = tex.GetPixels32();
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
    
    [Serializable]
    public class CacheEntry
    {
        public string DefName;
        public Color Color;
    }
}