using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Page_SelectStartingSite))]
internal class Patch_RimWorld_Page_SelectStartingSite
{
    [HarmonyPostfix]
    [HarmonyPatch("PreOpen")]
    private static void PreOpen_Postfix() => WorldInterfaceManager.RefreshActive();
}
