using UnityEngine;

/// <summary>
/// Locks its RectTransform to the 16:9 reference aspect regardless of screen
/// resolution/aspect. Placed under the SafeArea, it sizes itself to the largest
/// 16:9 rectangle that fits the safe area and centers it, so the UI it contains
/// always keeps the exact reference proportions (a letterboxed "design" area).
/// Works in edit mode and play mode, and reacts to resolution / safe-area changes.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ReferenceRatioFitter : MonoBehaviour
{
    public const float ReferenceAspect = 16f / 9f;

    RectTransform rectTransform;
    Rect lastParentRect = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);

    void OnEnable()
    {
        rectTransform = (RectTransform)transform;
        Apply();
    }

    /// <summary>Recompute immediately (used by editor tooling / batch capture).</summary>
    public void ForceApply()
    {
        Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    void Update()
    {
        var parentRt = rectTransform != null ? rectTransform.parent as RectTransform : null;
        if (parentRt == null) return;
        var pr = parentRt.rect;
        if (pr.width <= 0f || pr.height <= 0f) return;
        if (!pr.Equals(lastParentRect)) Apply();
    }

    void Apply()
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        var parentRt = rectTransform.parent as RectTransform;
        if (parentRt == null) return;

        var pr = parentRt.rect;
        if (pr.width <= 0f || pr.height <= 0f) return;
        lastParentRect = pr;

        // Largest 16:9 rect that fits the parent (safe area).
        float w = pr.width;
        float h = w / ReferenceAspect;
        if (h > pr.height)
        {
            h = pr.height;
            w = h * ReferenceAspect;
        }

        // Center within the parent.
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(w, h);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
