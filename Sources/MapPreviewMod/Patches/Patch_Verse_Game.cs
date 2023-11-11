using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Main")]
[HarmonyPatch(typeof(Game))]
internal class Patch_Verse_Game
{
    [HarmonyPostfix]
    [HarmonyPatch("InitNewGame")]
    private static void InitNewGame_Postfix() => WorldInterfaceManager.RefreshInterface();

    [HarmonyPostfix]
    [HarmonyPatch("LoadGame")]
    private static void LoadGame_Postfix() => WorldInterfaceManager.RefreshInterface();
}
