using System;
using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

internal class ModCompat_RimThreaded : ModCompat
{
    public override string TargetAssemblyName => "RimThreaded";

    protected override bool OnApply()
    {
        var type = FindType("RimThreaded.RimThreaded");
        var method = Require(AccessTools.Method(type, "InitializeAllThreadStatics"));

        MapPreviewGenerator.OnPreviewThreadInit += () =>
        {
            method.Invoke(null, Array.Empty<object>());
        };
        
        return true;
    }
}