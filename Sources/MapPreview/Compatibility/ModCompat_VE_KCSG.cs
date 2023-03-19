using LunarFramework.Patching;

namespace MapPreview.Compatibility;

internal class ModCompat_VE_KCSG : ModCompat
{
    public override string TargetAssemblyName => "KCSG";
    public override string DisplayName => "Vanilla Expanded Framework";

    protected override bool OnApply()
    {
        MapPreviewRequest.AddDefaultGenStepPredicate(def => def.defName == "KCSG_TerrainNoPatches");
        return true;
    }
}
