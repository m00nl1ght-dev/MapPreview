using LunarFramework;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

namespace MapPreview;

public class ModInstance : Mod
{
    internal static readonly LunarAPI LunarAPI = LunarAPI.Create("Map Preview Mod", Init, Cleanup);
    
    internal static PatchGroup MainPatchGroup;
    internal static PatchGroup CompatPatchGroup;
    
    internal static Settings Settings;

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(ModInstance).Assembly);
        MainPatchGroup.Subscribe();
        
        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        ModCompat.ApplyAll(LunarAPI, CompatPatchGroup);
        
        Main.AddStableSeedCondition(map => Settings.SkipRiverFlowCalc && map.TileInfo.Rivers?.Count > 0);
    }
    
    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
    }

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