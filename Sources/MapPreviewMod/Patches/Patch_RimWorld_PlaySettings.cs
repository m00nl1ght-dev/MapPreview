using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Active")]
[HarmonyPatch(typeof(PlaySettings))]
internal class Patch_RimWorld_PlaySettings
{
    [HarmonyPostfix]
    [HarmonyPatch("DoPlaySettingsGlobalControls")]
    private static void DoPlaySettingsGlobalControls(WidgetRow row, bool worldView)
    {
        if (worldView)
        {
            bool current = MapPreviewMod.Settings.PreviewEnabledNow, prev = current;

            row.ToggleableIcon(ref current, TexButton.Info, "MapPreview.World.ShowHidePreview".Translate(), SoundDefOf.Mouseover_ButtonToggle);

            if (prev != current)
            {
                if (Current.ProgramState == ProgramState.Entry)
                {
                    MapPreviewMod.Settings.EnableMapPreview.Value = current;
                }
                else
                {
                    MapPreviewMod.Settings.EnableMapPreviewInPlay.Value = current;
                }

                WorldInterfaceManager.RefreshPreview();

                if (!MapPreviewMod.Settings.PreviewEnabledNow)
                {
                    MapPreviewWindow.Instance?.Close();
                }
            }
        }
    }
}
