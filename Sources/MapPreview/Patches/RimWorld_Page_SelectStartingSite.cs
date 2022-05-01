using HarmonyLib;
using MapReroll;
using RimWorld;
using Verse;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(Page_SelectStartingSite))]
public class RimWorld_Page_SelectStartingSite
{
    [HarmonyPatch("CanDoNext")]
    private static bool Prefix(bool __result)
    {
        if (Main.IsGeneratingPreview)
        {
            __result = false;
            Messages.Message("MapPreview.World.WaitForPreview".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        return true;
    }
}