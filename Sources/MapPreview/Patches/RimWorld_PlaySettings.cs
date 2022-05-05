using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(PlaySettings))]
public class RimWorld_PlaySettings
{
    [HarmonyPatch("DoPlaySettingsGlobalControls")]
    private static void Postfix(WidgetRow row, bool worldView)
    {
        if (worldView)
        {
            bool prev = ModInstance.Settings.EnableMapPreview;
            row.ToggleableIcon(ref ModInstance.Settings.EnableMapPreview, TexButton.TogglePauseOnError, (string) "MapPreview.World.ShowHidePreview".Translate(), SoundDefOf.Mouseover_ButtonToggle);
            if (prev != ModInstance.Settings.EnableMapPreview) RimWorld_WorldInterface.Refresh();
        }
    }
}