using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LunarFramework.Logging;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace MapPreview;

[StaticConstructorOnStartup]
public class TrueTerrainColors
{
    private static string CacheFile => Path.Combine(GenFilePaths.ConfigFolderPath, "TrueTerrainColorsCache.xml");

    private static readonly IngameLogContext Logger = new(typeof(TrueTerrainColors));

    private static readonly HashSet<string> ExcludeList = new() { "fluffy.stuffedfloors" };

    // Default colors from Map Reroll as fallback
    public static readonly IReadOnlyDictionary<string, Color> DefaultColors = new Dictionary<string, Color>
    {
        { "Sand", GenColor.FromHex("806F54") },
        { "Soil", GenColor.FromHex("6D5B49") },
        { "MarshyTerrain", GenColor.FromHex("3F412B") },
        { "SoilRich", GenColor.FromHex("42362A") },
        { "Gravel", GenColor.FromHex("6D5B49") },
        { "Mud", GenColor.FromHex("403428") },
        { "Marsh", GenColor.FromHex("363D30") },
        { "MossyTerrain", GenColor.FromHex("6D5B49") },
        { "Ice", GenColor.FromHex("9CA7AC") },
        { "WaterDeep", GenColor.FromHex("3A434D") },
        { "WaterOceanDeep", GenColor.FromHex("3A434D") },
        { "WaterMovingDeep", GenColor.FromHex("3A434D") },
        { "WaterShallow", GenColor.FromHex("434F50") },
        { "WaterOceanShallow", GenColor.FromHex("434F50") },
        { "WaterMovingShallow", GenColor.FromHex("434F50") }
    };

    public static IReadOnlyDictionary<string, Color> TrueColors => _trueColors ?? DefaultColors;

    private static Dictionary<string, Color> _trueColors;

    static TrueTerrainColors()
    {
        Logger.IgnoreLogLimitLevel = LogContext.LogLevel.Warn;

        if (File.Exists(CacheFile))
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(List<CacheEntry>));
                using var streamReader = File.OpenText(CacheFile);
                if (xmlSerializer.Deserialize(streamReader) is List<CacheEntry> cacheData)
                {
                    _trueColors = new Dictionary<string, Color>();
                    foreach (var cacheEntry in cacheData) _trueColors.Add(cacheEntry.DefName, cacheEntry.Color);
                    Logger.Log($"Loaded cached true colors for {cacheData.Count} terrain defs from file.");
                }
            }
            catch (Exception e)
            {
                _trueColors = null;
                Logger.Warn("Failed to read TrueTerrainColors cache from file.", e);
            }
        }

        try
        {
            CalculateTrueTerrainColors();
        }
        catch (Exception e)
        {
            _trueColors = null;
            Logger.Error("Unknown error occured while extracting colors from terrain textures. Using default colors as fallback.", e);
        }
    }

    public static void CalculateTrueTerrainColors(bool clear = false)
    {
        _trueColors ??= new Dictionary<string, Color>();
        if (clear) _trueColors.Clear();

        var missingDefs = DefDatabase<TerrainDef>.AllDefsListForReading
            .Where(def => !_trueColors.ContainsKey(def.defName) && !ExcludeList.Contains(def.modContentPack?.PackageId))
            .Where(def => def.graphic?.MatSingle?.mainTexture is Texture2D)
            .ToList();

        if (missingDefs.Count <= 0) return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var renderTex = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
        var readableTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        var previous = RenderTexture.active;
        RenderTexture.active = renderTex;

        foreach (var def in missingDefs)
        {
            if (def.graphic?.path?.EndsWith("BadTexture") == true)
            {
                _trueColors.Add(def.defName, Color.black);
                continue;
            }

            try
            {
                var texture = def.graphic?.MatSingle?.mainTexture;
                if (texture is Texture2D texture2D)
                {
                    Graphics.Blit(texture2D, renderTex);

                    readableTex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                    readableTex.Apply(false);

                    var rawColor = readableTex.GetPixel(0, 0);
                    var combinedColor = rawColor * def.graphic.MatSingle.color * def.graphic.color;

                    _trueColors.Add(def.defName, combinedColor);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to extract true color from terrain {def.defName}.", e);
            }
        }

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        Object.Destroy(readableTex);

        stopwatch.Stop();

        var time = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
        Logger.Log($"Extracted true colors from {missingDefs.Count} terrain defs in {time} seconds.");

        try
        {
            var cacheEntries = _trueColors.Select(p => new CacheEntry { DefName = p.Key, Color = p.Value }).ToList();
            var xmlSerializer = new XmlSerializer(typeof(List<CacheEntry>));
            using var streamWriter = File.CreateText(CacheFile);
            xmlSerializer.Serialize(streamWriter, cacheEntries);
        }
        catch (Exception e)
        {
            Logger.Warn("Failed to write TrueTerrainColors cache to file", e);
        }
    }

    [Serializable]
    public class CacheEntry
    {
        public string DefName;
        public Color Color;
    }
}
