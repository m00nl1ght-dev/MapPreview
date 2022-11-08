using System;
using System.Collections.Generic;
using System.Linq;
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

internal class ModCompat_WorldEdit : ModCompat
{
    public override string TargetAssemblyName => "WorldEdit 2.0";
    public override string DisplayName => "WorldEdit 2.0";
    
    private FieldInfo _instance;
    private PropertyInfo _editors;
    private PropertyInfo _editorName;
    private FieldInfo _openedEditor;
    private MethodInfo _show;
    private MethodInfo _close;

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

        MapPreviewToolbar.ExtToolbar += DrawSettingsButton;
        MapPreviewToolbar.ExtraWidth(40);
        return true;
    }

    private void DrawSettingsButton(MapPreviewToolbar toolbar, LayoutRect layout)
    {
        GUI.enabled = !MapPreviewAPI.IsGeneratingPreview;
        var btnPos = layout.Abs();
        TooltipHandler.TipRegion(btnPos, "MapPreview.Integration.WorldEdit.Open".Translate());
        if (GUI.Button(btnPos, TexButton.GodModeDisabled))
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

        GUI.enabled = true;
    }
}