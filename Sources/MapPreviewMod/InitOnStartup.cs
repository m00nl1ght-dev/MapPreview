using HarmonyLib;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public static class InitOnStartup
{
    static InitOnStartup()
    {
        new Harmony("Map Preview Mod").PatchAll();
        
        TrueTerrainColors.EnabledFunc = () => ModInstance.Settings.EnableTrueTerrainColors;
    }
}