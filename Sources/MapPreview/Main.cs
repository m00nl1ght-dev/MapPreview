using HarmonyLib;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public static class Main
{
    static Main()
    {
        new Harmony("Map Preview").PatchAll();
    }
}