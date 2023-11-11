using HarmonyLib;
using LunarFramework.Patching;
using Verse.Profile;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(MemoryUtility))]
internal class Patch_Verse_MemoryUtility
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MemoryUtility.ClearAllMapsAndWorld))]
    private static void ClearAllMapsAndWorld_Postfix() => WorldInterfaceManager.UpdateWhileWorldHidden();
}
