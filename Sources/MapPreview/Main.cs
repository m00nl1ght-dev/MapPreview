using HarmonyLib;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public static class Main
{
    public static bool IsGeneratingPreview { get; set; }

    static Main()
    {
        new Harmony("Map Preview").PatchAll();
    }
}