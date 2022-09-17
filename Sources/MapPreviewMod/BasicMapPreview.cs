using System;
using LunarFramework.Logging;
using UnityEngine;
using Verse;

namespace MapPreview;

[StaticConstructorOnStartup]
public class BasicMapPreview : MapPreview
{
    private static readonly Texture2D _uiPreviewLoading = ContentFinder<Texture2D>.Get("UIPreviewLoadingMP");

    public BasicMapPreview(IntVec2 maxMapSize) : base(maxMapSize) {}

    protected override void DrawGenerating(Rect inRect)
    {
        DrawPreloader(_uiPreviewLoading, inRect.center);
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