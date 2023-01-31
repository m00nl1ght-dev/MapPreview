using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

namespace MapPreview.Compatibility;

internal class ModCompat_PrepareLanding : ModCompat
{
    public static bool IsPresent { get; private set; }

    public override string TargetAssemblyName => "PrepareLanding";
    public override string DisplayName => "Prepare Landing";

    private static PropertyInfo _instance;
    private static PropertyInfo _window;
    private static PropertyInfo _data;

    private static Type _windowType;
    private static Type _windowMinimizedType;

    protected override bool OnApply()
    {
        var type = FindType("PrepareLanding.PrepareLanding");

        _instance = Require(AccessTools.Property(type, "Instance"));
        _window = Require(AccessTools.Property(type, "MainWindow"));
        _data = Require(AccessTools.Property(type, "GameData"));

        _windowType = FindType("PrepareLanding.MainWindow");
        _windowMinimizedType = FindType("PrepareLanding.Core.Gui.Window.MinimizedWindow");

        MapPreviewToolbar.RegisterButton(new ButtonOpenPrepareLanding());

        IsPresent = true;
        return true;
    }

    private class ButtonOpenPrepareLanding : MapPreviewToolbar.Button
    {
        public override bool IsVisible => MapPreviewMod.Settings.EnablePrepareLandingIntegration;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview;

        public override string Tooltip => "MapPreview.Integration.PrepareLanding.Open".Translate();
        public override Texture Icon => TexButton.Search;

        public override void OnAction()
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
                    windowStack.Windows.FirstOrDefault(w => w.GetType() == _windowMinimizedType)?.Close();
                    windowStack.Add(window);
                }
            }
        }
    }
}
