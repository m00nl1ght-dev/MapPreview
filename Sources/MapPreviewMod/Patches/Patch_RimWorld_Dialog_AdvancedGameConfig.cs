using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Active")]
[HarmonyPatch(typeof(Dialog_AdvancedGameConfig))]
internal class Patch_RimWorld_Dialog_AdvancedGameConfig
{
    [HarmonyPostfix]
    [HarmonyPatch("DoWindowContents")]
    private static void DoWindowContents()
    {
        var world = Find.World;
        if (!GUI.changed && world != null) return;

        var currentPreviewMap = MapPreviewWindow.Instance?.CurrentPreviewMap;
        if (currentPreviewMap != null)
        {
            var newMapSize = MapPreviewWindow.DetermineMapSize(world, currentPreviewMap.Tile);
            if (currentPreviewMap.Size != new IntVec3(newMapSize.x, currentPreviewMap.Size.y, newMapSize.z))
            {
                WorldInterfaceManager.RefreshPreview();
            }
        }
    }
}
