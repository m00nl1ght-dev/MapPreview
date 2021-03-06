using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

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
            row.ToggleableIcon(ref ModInstance.Settings.EnableMapPreview, TexButton.TogglePauseOnError, "MapPreview.World.ShowHidePreview".Translate(), SoundDefOf.Mouseover_ButtonToggle);
            if (prev != ModInstance.Settings.EnableMapPreview) RimWorld_WorldInterface.Refresh();
        }
    }
}