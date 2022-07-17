using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace MapPreview;

public class ModInstance : Mod
{
    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;

    public static string LogPrefix => "[Map Preview v" + Version + "] ";
    
    public static Settings Settings;

    public ModInstance(ModContentPack content) : base(content)
    {
        Settings = GetSettings<Settings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Map Preview";
}