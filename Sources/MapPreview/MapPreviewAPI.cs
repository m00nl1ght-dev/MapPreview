using System;
using System.Collections.Generic;
using LunarFramework;
using LunarFramework.Logging;
using LunarFramework.Patching;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public static class MapPreviewAPI
{
    // ### Init ###
    
    internal static readonly LunarAPI LunarAPI = LunarAPI.Create("Map Preview", Init, Cleanup);
    
    internal static LogContext Logger => LunarAPI.LogContext;
    
    internal static PatchGroup MainPatchGroup;
    internal static PatchGroup CompatPatchGroup;
    internal static PatchGroup GenPatchGroup;

    private static void Init()
    {
        MainPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Main");
        MainPatchGroup.AddPatches(typeof(MapPreviewAPI).Assembly);
        MainPatchGroup.Subscribe();

        CompatPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Compat");
        CompatPatchGroup.Subscribe();

        GenPatchGroup ??= LunarAPI.RootPatchGroup.NewSubGroup("Gen");
        GenPatchGroup.AddPatches(typeof(MapPreviewAPI).Assembly);

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
    
    // ### Public API ###
    
    public static bool IsReady => MainPatchGroup is { Active: true } && GenPatchGroup != null;
    public static bool IsReadyForPreviewGen => IsReady && GenPatchGroup.Active;
    public static bool IsGeneratingPreview { get; internal set; }

    private static readonly List<Predicate<Map>> StableSeedConditions = new();
    
    public static bool ShouldUseStableSeed(Map map) => StableSeedConditions.Any(c => c.Invoke(map));

    public static void AddStableSeedCondition(Predicate<Map> condition)
    {
        StableSeedConditions.Add(condition);
    }
    
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