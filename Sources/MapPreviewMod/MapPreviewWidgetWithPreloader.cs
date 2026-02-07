using System;
using System.Linq;
using System.Threading;
using LunarFramework.Logging;
using UnityEngine;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public class MapPreviewWidgetWithPreloader : MapPreviewWidget
{
    public static readonly Texture2D UIPreviewLoading = ContentFinder<Texture2D>.Get("UIPreviewLoadingMP");
    public static readonly Texture2D UIPreviewReset = ContentFinder<Texture2D>.Get("UIPreviewResetMP");

    public MapPreviewWidgetWithPreloader(IntVec2 maxMapSize) : base(maxMapSize) { }

    protected override void DrawGenerated(Rect inRect)
    {
        var animate = MapPreviewMod.Settings.EnablePreviewAnimations.Value;
        var texRect = animate ? inRect.ScaledBy(SpawnInterpolator.value) : inRect;
        GUI.DrawTextureWithTexCoords(texRect.ContractedBy(1f), Texture, TexCoords);
    }

    protected override void DrawGenerating(Rect inRect)
    {
        DrawPreloader(UIPreviewLoading, inRect.center);
    }

    protected override void HandleError(Exception ex)
    {
        if (ex is ThreadAbortException or NotSupportedException) return;

        if (ex is ArgumentNullException)
        {
            if (DefDatabase<FleckDef>.AllDefs.Any(d => d.fleckSystemClass == null))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "Map preview generation failed because one of your mods is broken. " +
                    "Please validate your mod files and make sure that your load order is correct. " +
                    "Most importantly, do not put any mods above 'Core' unless they specifically say so in their description!"
                ));

                return;
            }
        }

        Find.WindowStack.Add(new Dialog_MessageBox(
            "MapPreview.PreviewGenerationFailed".Translate(),
            null, () =>
            {
                LogPublisher.TryShowPublishPrompt();
            }
        ));
    }
}
