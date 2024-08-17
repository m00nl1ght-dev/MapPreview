using System;
using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
using LunarFramework.Utility;
using Verse;

namespace MapPreview;

[LunarComponentEntrypoint]
public static class MapPreviewAPI
{
    // ### Init ###

    internal static readonly LunarAPI LunarAPI = LunarAPI.Create("Map Preview", Init, Cleanup);

    internal static LogContext Logger => LunarAPI.LogContext;

    internal static PatchGroup MainPatchGroup;
    internal static PatchGroup CompatPatchGroup;
    internal static PatchGroup GenLowPatchGroup;
    internal static PatchGroup GenPatchGroup;

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(MapPreviewAPI).Assembly);
        MainPatchGroup.Subscribe();

        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        GenLowPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("GenLow");
        GenLowPatchGroup.AddPatches(typeof(MapPreviewAPI).Assembly);

        GenPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Gen");
        GenPatchGroup.AddPatches(typeof(MapPreviewAPI).Assembly);

        ModCompat.ApplyAll(LunarAPI, CompatPatchGroup);

        MainPatchGroup.CheckForConflicts(Logger);
        GenLowPatchGroup.CheckForConflicts(Logger);

        if (Logger is IngameLogContext ingameLogger)
        {
            ingameLogger.IgnoreLogLimitLevel = LogContext.LogLevel.Error;
        }
    }

    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
        GenLowPatchGroup?.UnsubscribeAll();
        GenPatchGroup?.UnsubscribeAll();
    }

    // ### Public API ###

    public static bool IsReady => MainPatchGroup is { Applied: true } && GenLowPatchGroup != null && GenPatchGroup != null;
    public static bool IsReadyForPreviewGen => IsReady && GenLowPatchGroup.Applied && GenPatchGroup.Applied;
    public static bool IsGeneratingPreview { get; internal set; }

    public static readonly ExtensionPoint<Map, bool> ShouldUseStableSeed = new(false);

    public static void AddStableSeedCondition(Predicate<Map> condition)
    {
        ShouldUseStableSeed.AddModifier(1, (map, val) => val || condition(map));
    }

    public static event Action OnWorldChanged;

    public static void NotifyWorldChanged()
    {
        OnWorldChanged?.Invoke();
    }

    public static void SubscribeGenPatches(PatchGroupSubscriber subscriber)
    {
        GenLowPatchGroup?.Subscribe(subscriber);
        GenPatchGroup?.Subscribe(subscriber);
    }

    public static void UnsubscribeGenPatches(PatchGroupSubscriber subscriber)
    {
        GenLowPatchGroup?.Unsubscribe(subscriber);
        GenPatchGroup?.Unsubscribe(subscriber);
    }

    public static void UnsubscribeGenPatchesFast(PatchGroupSubscriber subscriber)
    {
        GenPatchGroup?.Unsubscribe(subscriber);
    }
}
