using System;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_GeologicalLandforms : ModCompat
{
    public static bool IsPresent { get; private set; }

    public override string TargetAssemblyName => "GeologicalLandformsMod";
    public override string DisplayName => "Geological Landforms";

    private static Type _modType;
    private static Mod _mod;

    protected override bool OnApply()
    {
        _modType = FindType("GeologicalLandforms.GeologicalLandformsMod");
        _mod = Require(LoadedModManager.GetMod(_modType));

        MapPreviewToolbar.RegisterButton(new ButtonOpenGeologicalLandforms());

        IsPresent = true;
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("GeologicalLandforms.GraphEditor.LandformGraphEditor", "Init")]
    private static void LandformGraphEditor_Init_Prefix()
    {
        MapPreviewWindow.Instance?.Close();
        MapPreviewToolbar.Instance?.Close();
    }

    private class ButtonOpenGeologicalLandforms : MapPreviewToolbar.Button
    {
        public override bool IsVisible => MapPreviewMod.Settings.EnableLandformSettingsIntegration;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview;

        public override string Tooltip => "MapPreview.Integration.LandformSettings.Open".Translate();
        public override Texture Icon => TexButton.ShowTerrainAffordanceOverlay;

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
