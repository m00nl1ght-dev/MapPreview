
#if RW_1_6_OR_GREATER

using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(GenStep_Asteroid))]
internal static class Patch_RimWorld_GenStep_Asteroid
{
    [HarmonyPrefix]
    [HarmonyPatch("GenerateRuins")]
    private static bool GenerateRuins()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }

    [HarmonyPrefix]
    [HarmonyPatch("GenerateArcheanTree")]
    private static bool GenerateArcheanTree()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SpawnOres")]
    private static bool SpawnOres()
    {
        return !MapPreviewAPI.IsGeneratingPreview || !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}

#endif
