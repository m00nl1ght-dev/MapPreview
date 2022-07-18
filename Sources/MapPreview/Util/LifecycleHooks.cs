using System;

namespace MapPreview.Util;

public static class LifecycleHooks
{
    public static event Action OnWorldChanged;
    internal static void NotifyWorldChanged()
    {
        OnWorldChanged?.Invoke();
    }
    
    public static event Action OnUpdateOnce;
    internal static void RunOnUpdateOnce()
    {
        OnUpdateOnce?.Invoke();
        OnUpdateOnce = null;
    }

    public static event Action OnShutdown;
    internal static void RunOnShutdown()
    {
        OnShutdown?.Invoke();
        OnShutdown = null;
    }
}