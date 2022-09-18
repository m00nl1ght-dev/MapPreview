using HarmonyLib;
using LunarFramework.Patching;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Compatibility;

internal class ModCompat_BetterMapSizes : ModCompat
{
    public override string TargetAssemblyName => "CustomMapSizes";
    public override string DisplayName => "Better Map Sizes";

    protected override bool OnApply()
    {
        var type = FindType("CustomMapSizes.CustomMapSizesMain");
        
        var mapWidth  = Require(AccessTools.Field(type, "mapWidth"));
        var mapHeight = Require(AccessTools.Field(type, "mapHeight"));
        
        if ((int) mapWidth.GetValue(null) < 0 || (int) mapHeight.GetValue(null) < 0) return false;

        MapPreviewWindow.MaxMapSize = new IntVec2(1000, 1000);
        MapPreviewWindow.MapSizeOverride = () => new IntVec2((int) mapWidth.GetValue(null), (int) mapHeight.GetValue(null));

        return true;
    }
}