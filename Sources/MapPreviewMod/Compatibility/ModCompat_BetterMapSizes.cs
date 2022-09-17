using System.Reflection;
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
    public override string TargetAssembly => "CustomMapSizes";
    public override string DisplayName => "Better Map Sizes";

    protected override bool OnApply(Assembly assembly)
    {
        var type = assembly.GetType("CustomMapSizes.CustomMapSizesMain", true);
        
        var mapWidth  = AccessTools.Field(type, "mapWidth");
        var mapHeight = AccessTools.Field(type, "mapHeight");
        
        if (mapWidth == null || mapHeight == null) return false;
        if ((int) mapWidth.GetValue(null) < 0 || (int) mapHeight.GetValue(null) < 0) return false;

        MapPreviewWindow.MaxMapSize = new IntVec2(1000, 1000);
        MapPreviewWindow.MapSizeOverride = () => new IntVec2((int) mapWidth.GetValue(null), (int) mapHeight.GetValue(null));

        return true;
    }
}