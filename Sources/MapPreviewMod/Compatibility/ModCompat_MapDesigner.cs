using System;
using System.Reflection;
using LunarFramework.Patching;
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
    public static bool IsPresent { get; private set; }
    
    public override string TargetAssemblyName => "MapDesigner";
    public override string DisplayName => "Map Designer";

    private static Type _modType;
    private static Mod _mod;

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
        
        MapPreviewToolbar.RegisterButton(new ButtonOpenMapDesigner());

        IsPresent = true;
        return true;
    }

    private void OnSettingsChanged()
    {
        MapPreviewAPI.NotifyWorldChanged();
    }

    private class ButtonOpenMapDesigner : MapPreviewToolbar.Button
    {
        public override bool IsVisible => MapPreviewMod.Settings.EnableMapDesignerIntegration;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview;

        public override string Tooltip => "MapPreview.Integration.MapDesigner.OpenSettings".Translate();
        public override Texture Icon => TexButton.InspectModeToggle;

        public override void OnAction()
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