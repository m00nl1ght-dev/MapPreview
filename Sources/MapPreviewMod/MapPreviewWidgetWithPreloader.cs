using System;
using LunarFramework.Logging;
using UnityEngine;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public class MapPreviewWidgetWithPreloader : MapPreviewWidget
{
    public static readonly Texture2D UIPreviewLoading = ContentFinder<Texture2D>.Get("UIPreviewLoadingMP");
    public static readonly Texture2D UIPreviewReset = ContentFinder<Texture2D>.Get("UIPreviewResetMP");

    public MapPreviewWidgetWithPreloader(IntVec2 maxMapSize) : base(maxMapSize) {}

    protected override void DrawGenerating(Rect inRect)
    {
        DrawPreloader(UIPreviewLoading, inRect.center);
    }

    protected override void HandleError(Exception ex)
    {
        Find.WindowStack.Add(new Dialog_MessageBox(
            "MapPreview.PreviewGenerationFailed".Translate(),
            null, () =>
            {
                LogPublisher.TryShowPublishPrompt();
            }
        ));
    }
}