using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LunarFramework.Patching;
using UnityEngine;
using Verse;

// ReSharper disable RedundantAssignment
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace MapPreview.Compatibility;

internal class ModCompat_WorldEdit : ModCompat
{
    public static bool IsPresent { get; private set; }
    
    public override string TargetAssemblyName => "WorldEdit 2.0";
    public override string DisplayName => "WorldEdit 2.0";
    
    private static FieldInfo _instance;
    private static PropertyInfo _editors;
    private static PropertyInfo _editorName;
    private static FieldInfo _openedEditor;
    private static MethodInfo _show;
    private static MethodInfo _close;

    protected override bool OnApply()
    {
        var type = FindType("WorldEdit_2_0.MainEditor.WorldEditor");
        var editorType = FindType("WorldEdit_2_0.MainEditor.Models.Editor");
        
        _instance = Require(AccessTools.Field(type, "worldEditorInstance"));
        _editors = Require(AccessTools.Property(type, "Editors"));
        _editorName = Require(AccessTools.Property(editorType, "EditorName"));
        _openedEditor = Require(AccessTools.Field(type, "openedEditor"));
        _show = Require(AccessTools.Method(editorType, "ShowEditor"));
        _close = Require(AccessTools.Method(editorType, "CloseEditor"));

        MapPreviewToolbar.RegisterButton(new ButtonOpenWorldEdit());

        IsPresent = true;
        return true;
    }

    private class ButtonOpenWorldEdit : MapPreviewToolbar.Button
    {
        public override bool IsVisible => MapPreviewMod.Settings.EnableWorldEditIntegration;
        public override bool IsInteractable => !MapPreviewAPI.IsGeneratingPreview;

        public override string Tooltip => "MapPreview.Integration.WorldEdit.Open".Translate();
        public override Texture Icon => TexButton.GodModeDisabled;

        public override void OnAction()
        {
            var instance = _instance.GetValue(null);
            if (instance != null && _editors.GetValue(instance) is IEnumerable<object> editorsEnumerable)
            {
                var editors = editorsEnumerable.ToList();
                var options = new List<FloatMenuOption>();
                var opened = _openedEditor.GetValue(instance);
                
                foreach (var editor in editors)
                {
                    options.Add(new FloatMenuOption(_editorName.GetValue(editor).ToString(), () =>
                    {
                        if (opened != null) _close.Invoke(opened, Array.Empty<object>());
                        _show.Invoke(editor, Array.Empty<object>());
                        _openedEditor.SetValue(instance, editor);
                    }));
                }
                
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}