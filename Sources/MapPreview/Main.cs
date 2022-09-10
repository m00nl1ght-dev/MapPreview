using System;
using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
using RimWorld;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public static class Main
{
    public static bool IsReady => MainPatchGroup is { Active: true } && GenPatchGroup != null;
    public static bool IsReadyForPreviewGen => IsReady && GenPatchGroup.Active;
    public static bool IsGeneratingPreview { get; internal set; }

    internal static readonly LunarAPI LunarAPI = LunarAPI.Create("Map Preview", Init, Cleanup);
    
    internal static LogContext Logger => LunarAPI.LogContext;
    
    internal static PatchGroup MainPatchGroup;
    internal static PatchGroup CompatPatchGroup;
    internal static PatchGroup GenPatchGroup;

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(Main).Assembly);
        MainPatchGroup.Subscribe();

        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        GenPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Gen", 3f);
        GenPatchGroup.AddPatches(typeof(Main).Assembly);

        ModCompat.ApplyAll(LunarAPI, CompatPatchGroup);

        if (Logger is IngameLogContext ingameLogger)
        {
            ingameLogger.IgnoreLogLimitLevel = LogContext.LogLevel.Error;
        }
    }
    
    private static void Cleanup()
    {
        MainPatchGroup?.UnsubscribeAll();
        CompatPatchGroup?.UnsubscribeAll();
        GenPatchGroup?.UnsubscribeAll();
    }
    
    public static Func<TerrainPatchMaker, int, int> TpmSeedSource;
    
    public static event Action OnWorldChanged;

    public static void NotifyWorldChanged()
    {
        OnWorldChanged?.Invoke();
    }

    public static void SubscribeGenPatches(PatchGroupSubscriber subscriber)
    {
        GenPatchGroup?.Subscribe(subscriber);
    }

    public static void UnsubscribeGenPatches(PatchGroupSubscriber subscriber)
    {
        GenPatchGroup?.Unsubscribe(subscriber);
    }
}