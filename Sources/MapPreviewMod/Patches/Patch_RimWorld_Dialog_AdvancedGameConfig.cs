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
        if (!GUI.changed) return;

        var world = Find.World;
        var currentPreviewMap = MapPreviewWindow.Instance?.CurrentPreviewMap;

        if (world != null && currentPreviewMap != null)
        {
            var newMapSize = MapSizeUtility.DetermineMapSize(world, world.worldObjects.MapParentAt(currentPreviewMap.Tile));
            if (currentPreviewMap.Size != new IntVec3(newMapSize.x, currentPreviewMap.Size.y, newMapSize.z))
            {
                WorldInterfaceManager.RefreshPreview();
            }
        }
    }
}
