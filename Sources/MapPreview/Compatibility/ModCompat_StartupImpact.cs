using HarmonyLib;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

internal class ModCompat_StartupImpact : ModCompat
{
    public override string TargetAssemblyName => "StartupImpact";
    public override string DisplayName => "Startup Impact";

    protected override bool OnApply()
    {
        var type = FindType("StartupImpact.Patch.DeepProfilerStart");
        var field = Require(AccessTools.Field(type, "mute"));
        
        field.SetValue(null, true);
        
        MapPreviewGenerator.OnBeginGenerating += _ =>
        {
            field.SetValue(null, true);
        };

        return true;
    }
}