using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;

namespace MapPreview.Patches;

[PatchGroup("Active")]
[HarmonyPatch(typeof(WorldInterface))]
internal class Patch_RimWorld_WorldInterface
{
    [HarmonyPostfix]
    [HarmonyPatch("WorldInterfaceUpdate")]
    private static void WorldInterfaceUpdate(WorldSelector ___selector)
    {
        WorldInterfaceManager.Update(___selector);
    }
}
