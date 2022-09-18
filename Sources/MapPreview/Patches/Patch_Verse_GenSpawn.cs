using HarmonyLib;
using LunarFramework.Patching;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Gen")]
[HarmonyPatch(typeof(GenSpawn))]
internal static class Patch_Verse_GenSpawn
{
    [HarmonyPrefix]
    [HarmonyPatch("Spawn", typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool))]
    private static bool Spawn(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
    {
        if (!Main.IsGeneratingPreview) return true;
        var generating = MapPreviewGenerator.GeneratingMapOnCurrentThread;
        if (generating != map) return true;
        
        Main.Logger.Warn("Some mod attempted to spawn thing " + newThing + " on a preview map, this is not supported!");
        return true;
    }
}