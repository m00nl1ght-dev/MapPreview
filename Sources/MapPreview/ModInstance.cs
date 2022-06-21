using System.Diagnostics;
using System.Reflection;
using MapPreview.Patches;
using UnityEngine;
using Verse;

namespace MapPreview;

public class ModInstance : Mod
{
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public static string LogPrefix => "[Map Preview v" + Version + "] ";
    
    public static Settings Settings;

    public ModInstance(ModContentPack content) : base(content)
    {
        Settings = GetSettings<Settings>();
        ModCompat_PerformanceOptimizer.Apply();
        ModCompat_GeologicalLandforms.Apply();
        ModCompat_SmashTools.Apply();
        ModCompat_RimThreaded.Apply();
        ModCompat_MapReroll.Apply();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Map Preview";
}