using HarmonyLib;
using MapReroll;
using RimWorld;
using Verse;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(TerrainGrid))]
public class RimWorld_TerrainGrid
{
    [HarmonyPatch("DoTerrainChangedEffects")]
    private static bool Prefix()
    {
        return !Main.IsGeneratingPreview;
    }
}