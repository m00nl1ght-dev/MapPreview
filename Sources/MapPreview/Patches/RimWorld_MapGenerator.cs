using HarmonyLib;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[HarmonyPatch(typeof(MapGenerator))]
internal static class RimWorld_MapGenerator
{
    [HarmonyPatch(nameof(MapGenerator.GenerateContentsIntoMap))]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Map map, int seed)
    {
        RimWorld_TerrainPatchMaker.Reset();
    }
    
    [HarmonyPatch(nameof(MapGenerator.GenerateContentsIntoMap))]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Map map, int seed)
    {
        RimWorld_TerrainPatchMaker.Reset();
    }
}