using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

namespace MapPreview;

[LunarComponentEntrypoint]
public class MapPreviewMod : Mod
{
    internal static readonly LunarAPI LunarAPI = LunarAPI.Create("Map Preview Mod", Init, Cleanup);

    internal static LogContext Logger => LunarAPI.LogContext;

    internal static PatchGroup MainPatchGroup;
    internal static PatchGroup ActivePatchGroup;
    internal static PatchGroup CompatPatchGroup;

    internal static MapPreviewSettings Settings;

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(MapPreviewMod).Assembly);
        MainPatchGroup.Subscribe();

        ActivePatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Active");
        ActivePatchGroup.AddPatches(typeof(MapPreviewMod).Assembly);

        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        ModCompat.ApplyAll(LunarAPI, CompatPatchGroup);

        MainPatchGroup.CheckForConflicts(Logger);
        ActivePatchGroup.CheckForConflicts(Logger);

        MapPreviewRequest.AddDefaultGenStepPredicate(s => s.defName == "Caves" && Settings.IncludeCaves);

        #if !RW_1_6_OR_GREATER
        MapPreviewAPI.AddStableSeedCondition(map => Settings.SkipRiverFlowCalc && map.TileInfo.Rivers?.Count > 0);
        #endif
    }

    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        ActivePatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
    }

    public MapPreviewMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<MapPreviewSettings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Map Preview";
}
