#if RW_1_6_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Verse;

namespace MapPreview;

public static class RandCompatCache
{
    public const uint ExpectedRandIterationsInVanillaMapComponents = 1U; // GasGrid ctor does one Rand call

    private const string PreFillModeKey = "MapPreview.RandCompatCache.PreFillMode";
    private const uint PreFillModeVanilla = 1U;
    private const uint PreFillModeRealConstruction = 2U;

    private static string CacheFile => Path.Combine(GenFilePaths.ConfigFolderPath, "MapPreviewCompatCache.xml");

    private static readonly Dictionary<string, uint> KnownConsistentDefaults = new()
    {
        { "ZombieLand.ZombieWeather", 8 }, // brrainz.zombieland
        { "MultiFloors.MF_LevelMapComp", 1 }, // telardo.multifloors
        { "CultOfCthulhu.MapComponent_LocalCultTracker", 2 }, // zal.cocc
        { "OberoniaAureaGene.Snowstorm.MapComponent_Snowstorm", 2 }, // OARK.RatkinFaction.ScenarioExpand.Snowstorm
    };

    private static Dictionary<string, uint> ExpectedRandIterations = new(KnownConsistentDefaults);

    public static bool TryGetExpectedInterations(Type mapComponentType, out uint iterations)
    {
        return ExpectedRandIterations.TryGetValue(KeyForType(mapComponentType), out iterations);
    }

    public static void UpdateExpectedIterations(Type mapComponentType, uint actualIt)
    {
        ExpectedRandIterations[KeyForType(mapComponentType)] = actualIt;
    }

    public static bool UseMinimalPreFillSkip =>
        ExpectedRandIterations.TryGetValue(PreFillModeKey, out var mode) && mode == PreFillModeVanilla;

    /// <summary>
    /// Records the Rand iteration count at the start of Map.FillComponents.
    /// Vanilla GasGrid.ctor is one Rand call. ConstructMinimalMapComponents leaves that object commented out (trial) and skips one iteration in its place.
    /// Any other count means Map.ConstructComponents called Rand again before FillComponents, so a one-iteration skip is a different RNG state than the map.
    /// Later minimal previews then run ConstructComponents. A later count of one does not switch back: one map that misses those calls must not re-enable the skip for the next map.
    /// </summary>
    public static void NotePreFillIterations(uint observed)
    {
        var known = ExpectedRandIterations.TryGetValue(PreFillModeKey, out var mode);
        if (known && mode == PreFillModeRealConstruction) return;

        if (observed == ExpectedRandIterationsInVanillaMapComponents)
        {
            if (known) return;
            ExpectedRandIterations[PreFillModeKey] = PreFillModeVanilla;
            Save();
            return;
        }

        var skippedVanillaCount = known && mode == PreFillModeVanilla;
        ExpectedRandIterations[PreFillModeKey] = PreFillModeRealConstruction;
        Save();

        if (skippedVanillaCount)
        {
            MapPreviewAPI.Logger.Warn(
                $"Map construction changed the RNG by {observed} iterations before map components. " +
                $"Minimal previews were skipping the single GasGrid roll. " +
                $"Later previews will run real map construction. Previews generated until now may have been inaccurate.");
        }
    }

    private static string KeyForType(Type type) => type.FullName ?? type.Name;

    public static void Load()
    {
        if (File.Exists(CacheFile))
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(List<CacheEntry>));

                using var streamReader = File.OpenText(CacheFile);

                if (xmlSerializer.Deserialize(streamReader) is List<CacheEntry> cacheData)
                {
                    ExpectedRandIterations = new Dictionary<string, uint>();

                    foreach (var cacheEntry in cacheData)
                        ExpectedRandIterations.Add(cacheEntry.TypeName, cacheEntry.RandIterations);
                }
            }
            catch (Exception e)
            {
                MapPreviewAPI.Logger.Warn("Failed to read compatibility cache data from file.", e);
            }
        }
    }

    public static void Save()
    {
        try
        {
            var cacheEntries = ExpectedRandIterations
                .Select(p => new CacheEntry { TypeName = p.Key, RandIterations = p.Value })
                .ToList();

            var xmlSerializer = new XmlSerializer(typeof(List<CacheEntry>));

            using var streamWriter = File.CreateText(CacheFile);
            xmlSerializer.Serialize(streamWriter, cacheEntries);
        }
        catch (Exception e)
        {
            MapPreviewAPI.Logger.Warn("Failed to write compatibility cache data to file.", e);
        }
    }

    [Serializable]
    public class CacheEntry
    {
        public string TypeName;
        public uint RandIterations;
    }
}

#endif
