using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LunarFramework.Patching;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapPreview.Compatibility;

internal class ModCompat_MapDesigner : ModCompat
{
    public static bool IsPresent { get; private set; }

    public override string TargetAssemblyName => "MapDesigner";
    public override string DisplayName => "Map Designer";

    private const int DebounceTime = 200;

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

    private static readonly Stopwatch _debouncer = new();

    private void OnSettingsChanged()
    {
        var running = _debouncer.IsRunning;

        _debouncer.Restart();

        if (!running)
        {
            MapPreviewMod.LunarAPI.LifecycleHooks.DoEnumerator(Debounce());
        }
    }

    private IEnumerator Debounce()
    {
        while (_debouncer.IsRunning && _debouncer.ElapsedMilliseconds < DebounceTime)
            yield return null;

        _debouncer.Reset();

        MapPreviewAPI.NotifyWorldChanged();
    }

    #if RW_1_6_OR_GREATER
    private static bool CanUseOnTile(PlanetTile tile)
    #else
    private static bool CanUseOnTile(int tile)
    #endif
    {
        if (tile < 0) return true;
        var mapParent = Find.WorldObjects?.MapParentAt(tile);
        if (mapParent == null) return true;
        var genSteps = mapParent.MapGeneratorDef?.genSteps;
        if (genSteps == null) return false;
        return RequiredGenSteps.All(name => genSteps.Any(def => def.defName == name));
    }

    private static readonly List<string> RequiredGenSteps = new()
    {
        "Terrain", "ElevationFertility"
    };

    private class ButtonOpenMapDesigner : MapPreviewToolbar.Button
    {
        public override bool IsVisible => MapPreviewMod.Settings.EnableMapDesignerIntegration;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview;

        public override string Tooltip => "MapPreview.Integration.MapDesigner.OpenSettings".Translate();
        public override Texture Icon => TexButton.OpenStatsReport;

        public override void OnAction()
        {
            var windowStack = Find.WindowStack;
            var existing = windowStack.WindowOfType<Dialog_ModSettings>();

            if (existing != null)
            {
                existing.Close();
            }
            else if (CanUseOnTile(MapPreviewWindow.CurrentTile))
            {
                windowStack.Add(new Dialog_ModSettings(_mod));
            }
            else
            {
                Messages.Message("MapPreview.Integration.MapDesigner.UnavailableForTile".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
