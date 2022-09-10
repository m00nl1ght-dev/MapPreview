using System;
using System.Reflection;
using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

internal class ModCompat_RimThreaded : ModCompat
{
    public override string TargetAssembly => "RimThreaded";
    public override string DisplayName => "RimThreaded";
    
    private MethodInfo InitializeAllThreadStatics;

    protected override bool OnApply(Assembly assembly)
    {
        var rtType = assembly.GetType("RimThreaded.RimThreaded");
        if (rtType == null) throw new Exception("RimThreaded version incompatible");
        
        InitializeAllThreadStatics = AccessTools.Method(rtType, "InitializeAllThreadStatics");
        if (InitializeAllThreadStatics == null) throw new Exception("InitializeAllThreadStatics not found");

        MapPreviewGenerator.OnPreviewThreadInit += InitThread;
        return true;
    }

    private void InitThread()
    {
        InitializeAllThreadStatics.Invoke(null, Array.Empty<object>());
    }
}