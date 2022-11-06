using System;
using System.Reflection;
using LunarFramework.GUI;
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
        
        MapPreviewToolbar.ExtToolbar += DrawSettingsButton;

        return true;
    }

    private void OnSettingsChanged()
    {
        MapPreviewAPI.NotifyWorldChanged();
    }

    private void DrawSettingsButton(MapPreviewToolbar toolbar, LayoutRect layout)
    {
        GUI.enabled = !MapPreviewAPI.IsGeneratingPreview;
        var btnPos = layout.Abs();
        TooltipHandler.TipRegion(btnPos, "MapPreview.Integration.MapDesigner.OpenSettings".Translate());
        if (GUI.Button(btnPos, TexButton.InspectModeToggle))
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

        GUI.enabled = true;
    }
}