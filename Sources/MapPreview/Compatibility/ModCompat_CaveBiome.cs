using System.Collections.Generic;
using LunarFramework.Patching;

namespace MapPreview.Compatibility;

internal class ModCompat_CaveBiome : ModCompat
{
    public override string TargetAssemblyName => "CaveBiome";

    protected override bool OnApply()
    {
        var mcp = ModContentPack;
        MapPreviewRequest.AddDefaultGenStepPredicate(def => def.modContentPack == mcp && DefNames.Contains(def.defName));
        return true;
    }

    private static readonly List<string> DefNames = new()
    {
        "CaveElevation", "CaveRiver",
    };
}
