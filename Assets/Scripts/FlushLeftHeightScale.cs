using UnityEngine;

/// <summary>
/// Pins its child-sized panel to the TRUE left edge of the screen (its parent,
/// typically the SafeArea root) and scales everything under it by screen height
/// relative to a fixed 1080p design height. Unlike a 16:9 letterbox fitter this
/// keeps contact with the physical screen edge on any aspect (ultrawide, 16:9,
/// 4:3, portrait), while button size/spacing stay identical relative to screen
/// height - so the menu reads the same on every device.
///
/// The resulting on-screen size is clamped to [minScale, maxScale] so the menu
/// never becomes tiny on small windows nor oversized on very large / portrait
/// displays; it still updates live as the screen changes.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FlushLeftHeightScale : MonoBehaviour
{
    public const float ReferenceHeight = 1080f;

    [Tooltip("Left margin in reference-design pixels (0 = flush with the screen edge).")]
    public float leftMargin = 0f;

    [Tooltip("Minimum on-screen scale relative to the 1080p design (floor).")]
    public float minScale = 0.85f;

    [Tooltip("Maximum on-screen scale relative to the 1080p design (ceiling).")]
    public float maxScale = 1.6f;

    RectTransform rectTransform;
    Vector2Int lastScreen = new Vector2Int(-1, -1);
    Rect lastParentRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);

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
        if (!pr.Equals(lastParentRect)) Apply();
    }

    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        var parentRt = rectTransform.parent as RectTransform;
        if (parentRt == null) return;

        var pr = parentRt.rect;
        if (pr.height <= 0f) return;
        lastParentRect = pr;
        lastScreen = new Vector2Int(Screen.width, Screen.height);

        // Local scale that yields a net on-screen size of screenHeight/1080. The
        // canvas scaler already applies canvas.scaleFactor, so the NET size is
        // parentHeight/1080 * canvasScaleFactor. Clamp that net size, then fold the
        // correction back into the local scale so the menu never gets too small
        // (or too large) regardless of the window size.
        float parentS = pr.height / ReferenceHeight;
        var canvas = GetComponentInParent<Canvas>();
        float canvasScale = (canvas != null && canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;
        float net = parentS * canvasScale;
        float clamped = Mathf.Clamp(net, minScale, maxScale);
        float factor = net > 1e-4f ? clamped / net : 1f;

        float s = parentS * factor;

        // Left edge of the (safe-area) parent, scaled margins so the offset
        // shrinks/grows proportionally to the screen.
        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(leftMargin * s, 0f);
        rectTransform.localScale = new Vector3(s, s, 1f);
    }
}
