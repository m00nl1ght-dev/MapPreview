using System;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MapGenerator))]
internal static class Patch_Verse_MapGenerator
{
    [HarmonyPrefix]
    [HarmonyPatch("GenerateMap")]
    private static bool GenerateMap()
    {
        if (!Main.IsGeneratingPreview) return true;
        throw new Exception("Attempted to use MapGenerator while a map preview is being generated!");
    }
}