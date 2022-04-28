using HarmonyLib;
using MapReroll.UI;

// ReSharper disable All

namespace MapPreview.Patches;

[HarmonyPatch(typeof(MapRerollUIController))]
public class MapReroll_MapRerollUIController
{
    [HarmonyPatch("OnGUI")]
    private static bool Prefix()
    {
        return ModInstance.Settings.EnableMapReroll;
    }
}