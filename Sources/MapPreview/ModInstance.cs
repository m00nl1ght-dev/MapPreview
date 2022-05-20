using MapPreview.Patches;
using UnityEngine;
using Verse;

namespace MapPreview;

public class ModInstance : Mod
{
    public const string Version = "1.9.2";

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