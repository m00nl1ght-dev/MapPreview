using HarmonyLib;
using MapReroll;
using RimWorld;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(GenStep_Terrain))]
public class RimWorld_GenStepTerrain
{
    [HarmonyPatch("GenerateRiverLookupTexture")]
    private static bool Prefix()
    {
        return !Main.IsGeneratingPreview;
    }
}