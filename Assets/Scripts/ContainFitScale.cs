using UnityEngine;

/// <summary>
/// Pins its panel to the left edge of the parent (typically the SafeArea root)
/// and scales it with a CONTAIN-FIT so the whole panel always stays inside the
/// parent on any screen size or aspect (16:9, 4:3, portrait, ultrawide, small
/// windows).
///
/// FlushLeftHeightScale only scales by screen height, which overflows narrow
/// screens with wide content (e.g. the 980px About card on 1024x768). This
/// component fits BOTH axes: scale = min(widthFit, heightFit), where the width
/// fit also reserves the left margin. The net design-to-screen scale is then
/// clamped to [minScale, maxScale] so the panel never becomes tiny on small
/// windows nor oversized on very large displays.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ContainFitScale : MonoBehaviour
{
    [Tooltip("Left margin in reference-design pixels from the parent's left edge.")]
    public float leftMargin = 48f;

    [Tooltip("Breathing room in reference-design pixels kept from the parent's top, right and bottom edges.")]
    public float margin = 72f;

    [Tooltip("Minimum net scale (design px -> screen px) - floor for small windows.")]
    public float minScale = 0.45f;

    [Tooltip("Maximum net scale (design px -> screen px) - ceiling for large displays.")]
    public float maxScale = 1.3f;

    RectTransform rectTransform;
    Vector2Int lastScreen = new Vector2Int(-1, -1);
    Rect lastParentRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
    Vector2 lastSize = new Vector2(float.NaN, float.NaN);

    void OnEnable()
    {
        rectTransform = (RectTransform)transform;
        Apply();
    }

    /// <summary>Recompute immediately (used by editor tooling / batch capture).</summary>
    public void ForceApply() => Apply();

    void Update()
    {
        var parentRt = rectTransform != null ? rectTransform.parent as RectTransform : null;
        if (parentRt == null) return;
        if (Screen.width != lastScreen.x || Screen.height != lastScreen.y)
        {
            Apply();
            return;
        }
        var pr = parentRt.rect;
        if (pr.width <= 0f || pr.height <= 0f) return;
        if (!pr.Equals(lastParentRect) || !rectTransform.rect.size.Equals(lastSize)) Apply();
    }

    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        var parentRt = rectTransform.parent as RectTransform;
        if (parentRt == null) return;

        var pr = parentRt.rect;
        var my = rectTransform.rect;
        if (pr.width <= 0f || pr.height <= 0f) return;
        // Layout not ready yet (ContentSizeFitter has not measured the panel).
        if (my.width <= 1f || my.height <= 1f) return;

        lastParentRect = pr;
        lastSize = my.size;
        lastScreen = new Vector2Int(Screen.width, Screen.height);

        var canvas = GetComponentInParent<Canvas>();
        float canvasScale = (canvas != null && canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;

        // Contain-fit in parent (canvas) units: the scaled panel plus the scaled
        // margins (leftMargin + margin on x, margin top and bottom on y) must fit
        // the parent on both axes. Canvas scale cancels out here, but is needed
        // for the net-scale clamp below.
        float fitW = pr.width / (my.width + leftMargin + margin);
        float fitH = pr.height / (my.height + margin * 2f);
        float s = Mathf.Min(fitW, fitH);

        // Clamp the NET design->screen scale, then fold the correction back
        // into the local scale (same approach as FlushLeftHeightScale).
        float net = s * canvasScale;
        float clamped = Mathf.Clamp(net, minScale, maxScale);
        if (net > 1e-4f) s *= clamped / net;

        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(leftMargin * s, 0f);
        rectTransform.localScale = new Vector3(s, s, 1f);
    }
}
