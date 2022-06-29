using HarmonyLib;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public static class Main
{
    public static bool IsGeneratingPreview;

    static Main()
    {
        new Harmony("Map Preview").PatchAll();
    }
}