using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

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