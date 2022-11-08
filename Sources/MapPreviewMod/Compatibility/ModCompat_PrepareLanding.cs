using System;
using System.Reflection;
using HarmonyLib;
using LunarFramework.GUI;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Compatibility;

internal class ModCompat_PrepareLanding : ModCompat
{
    public override string TargetAssemblyName => "PrepareLanding";
    public override string DisplayName => "Prepare Landing";
    
    private PropertyInfo _instance;
    private PropertyInfo _window;
    private PropertyInfo _data;
    private Type _windowType;

    protected override bool OnApply()
    {
        var type = FindType("PrepareLanding.PrepareLanding");
        _instance = Require(AccessTools.Property(type, "Instance"));
        _window = Require(AccessTools.Property(type, "MainWindow"));
        _data = Require(AccessTools.Property(type, "GameData"));
        _windowType = FindType("PrepareLanding.MainWindow");

        MapPreviewToolbar.ExtToolbar += DrawSettingsButton;
        MapPreviewToolbar.ExtraWidth(40);
        return true;
    }

    private void DrawSettingsButton(MapPreviewToolbar toolbar, LayoutRect layout)
    {
        GUI.enabled = !MapPreviewAPI.IsGeneratingPreview;
        var btnPos = layout.Abs();
        TooltipHandler.TipRegion(btnPos, "MapPreview.Integration.PrepareLanding.Open".Translate());
        if (GUI.Button(btnPos, TexButton.Search))
        {
            var windowStack = Find.WindowStack;
            
            var instance = _instance.GetValue(null);
            if (instance != null)
            {
                var window = (Window) _window.GetValue(instance);
                if (window == null)
                {
                    window = (Window) Activator.CreateInstance(_windowType, _data.GetValue(instance));
                    _window.SetValue(instance, window);
                }

                if (windowStack.IsOpen(window))
                {
                    window.Close();
                }
                else
                {
                    windowStack.Add(window);
                }
            }
        }

        GUI.enabled = true;
    }
}