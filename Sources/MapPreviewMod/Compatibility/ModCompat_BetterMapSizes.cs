using HarmonyLib;
using LunarFramework.Patching;
using Verse;

namespace MapPreview.Compatibility;

internal class ModCompat_BetterMapSizes : ModCompat
{
    public override string TargetAssemblyName => "CustomMapSizes";
    public override string DisplayName => "Better Map Sizes";

    protected override bool OnApply()
    {
        var type = FindType("CustomMapSizes.CustomMapSizesMain");

        var mapWidth = Require(AccessTools.Field(type, "mapWidth"));
        var mapHeight = Require(AccessTools.Field(type, "mapHeight"));

        if ((int) mapWidth.GetValue(null) < 0 || (int) mapHeight.GetValue(null) < 0) return false;

        MapSizeUtility.MaxMapSize = new IntVec2(1000, 1000);
        MapSizeUtility.MapSizeOverride = () =>
        {
            if (Find.GameInitData?.mapSize != -1) return new IntVec2(-1, -1);
            return new IntVec2((int) mapWidth.GetValue(null), (int) mapHeight.GetValue(null));
        };

        return true;
    }
}
