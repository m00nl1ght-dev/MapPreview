using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public static class Main
{
    public static Version LibVersion => typeof(Main).Assembly.GetName().Version;

    public static string LogPrefix => "[Map Preview v" + LibVersion + "] ";
    
    public static Func<TerrainPatchMaker, int, int> TpmSeedSource;

    public static bool IsGeneratingPreview;

    static Main()
    {
        new Harmony("Map Preview").PatchAll();
    }
}