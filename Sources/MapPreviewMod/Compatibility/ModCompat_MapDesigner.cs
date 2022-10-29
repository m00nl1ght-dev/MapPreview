using System;
using System.Reflection;
using LunarFramework.Patching;
using MapPreview.Patches;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Compatibility;

internal class ModCompat_MapDesigner : ModCompat
{
    public override string TargetAssemblyName => "MapDesigner";
    public override string DisplayName => "Map Designer";

    private Type _modType;
    private Mod _mod;

    private Texture2D _btnTex;

    protected override bool OnApply()
    {
        _modType = FindType("MapDesigner.MapDesignerMod");
        _mod = Require(LoadedModManager.GetMod(_modType));

        var helperType = FindType("MapDesigner.HelperMethods");
        var eventInfo = helperType.GetEvent("OnSettingsChanged", BindingFlags.Static | BindingFlags.Public);

        if (eventInfo == null)
        {
            MapPreviewMod.Logger.Warn("Could not apply integration with Map Designer. Most likely you are using an old version of Map Designer.");
            return false;
        }
        
        eventInfo.AddEventHandler(null, OnSettingsChanged);
        
        MapPreviewWindow.ExtOnGUI += DrawSettingsButton;

        return true;
    }

    private void OnSettingsChanged()
    {
        Patch_RimWorld_WorldInterface.Refresh();
    }

    private void DrawSettingsButton(MapPreviewWindow window, Rect rect)
    {
        if (MapPreviewAPI.IsGeneratingPreview) return;
        
        var btnRow = rect.TopPartPixels(70);
        var btnPos = btnRow.LeftPartPixels(70).ContractedBy(15);
        TooltipHandler.TipRegion(btnPos, "MapPreview.Integration.MapDesigner.OpenSettings".Translate());
        if (GUI.Button(btnPos, _btnTex ??= ContentFinder<Texture2D>.Get(OptionCategoryDefOf.General.texPath)))
        {
            var windowStack = Find.WindowStack;
            var existing = windowStack.WindowOfType<Dialog_ModSettings>();
            
            if (existing != null)
            {
                existing.Close();
            }
            else
            {
                windowStack.Add(new Dialog_ModSettings(_mod));
            }
        }
    }
}