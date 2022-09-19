using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Page_SelectStartingSite))]
internal class Patch_RimWorld_Page_SelectStartingSite
{
    [HarmonyPrefix]
    [HarmonyPatch("CanDoNext")]
    private static bool CanDoNext(ref bool __result)
    {
        if (MapPreviewAPI.IsGeneratingPreview)
        {
            __result = false;
            Messages.Message("MapPreview.World.WaitForPreview".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        return true;
    }
}