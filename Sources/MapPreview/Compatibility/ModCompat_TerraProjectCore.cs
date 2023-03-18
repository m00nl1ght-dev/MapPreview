using System.Collections.Generic;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

internal class ModCompat_TerraProjectCore : ModCompat
{
    public override string TargetAssemblyName => "TerraCore";

    protected override bool OnApply()
    {
        var mcp = ModContentPack;
        MapPreviewRequest.AddDefaultGenStepPredicate(def => def.modContentPack == mcp && DefNames.Contains(def.defName));
        return true;
    }

    private static readonly List<string> DefNames = new()
    {
        "ElevationFertilityPost", "BetterCaves"
    };
}
