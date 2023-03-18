using System.Collections.Generic;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Compatibility;

internal class ModCompat_RimNauts : ModCompat
{
    public override string TargetAssemblyName => "RimNauts2";

    protected override bool OnApply()
    {
        var mcp = ModContentPack;
        MapPreviewRequest.AddDefaultGenStepPredicate(def => def.modContentPack == mcp && Suffixes.Any(s => def.defName.EndsWith(s)));
        return true;
    }

    private static readonly List<string> Suffixes = new()
    {
        "_ElevationFertility", "_Terrain", "_Stripes", "_Vacuum"
    };
}
