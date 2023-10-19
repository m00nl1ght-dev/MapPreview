using System;
using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Compatibility;

internal class ModCompat_ConfigurableMaps : ModCompat
{
    public override string TargetAssemblyName => "ConfigurableMaps";
    public override string DisplayName => "Configurable Maps";

    protected override bool OnApply()
    {
        var type = FindType("ConfigurableMaps.DefsUtil");
        var method = Require(AccessTools.Method(type, "Update"));

        MapPreviewGenerator.OnBeginGenerating += _ =>
        {
            method.Invoke(null, Array.Empty<object>());
        };

        return true;
    }
}
