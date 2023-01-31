using UnityEngine;

namespace MapPreview;

public abstract class MapPreviewOverlay
{
    public readonly MapPreviewWidget PreviewWidget;

    protected MapPreviewOverlay(MapPreviewWidget previewWidget)
    {
        PreviewWidget = previewWidget;
    }

    public abstract void Draw(Rect rect);

    public virtual void Update(MapPreviewResult result) { }

    public virtual void Reset() { }
}
