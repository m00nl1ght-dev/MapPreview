using HarmonyLib;
using MapPreview.Util;
using Verse;

namespace MapPreview.Patches;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

[HarmonyPatch(typeof(Root))]
internal static class RimWorld_Root
{
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    private static void Update()
    {
        LifecycleHooks.RunOnUpdateOnce();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("Shutdown")]
    private static void Root_Shutdown()
    {
        LifecycleHooks.RunOnShutdown();
    }
}