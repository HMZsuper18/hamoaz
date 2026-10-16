using UnityEngine;

[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rectTransform;
    Rect currentSafeArea = new Rect(-1, -1, -1, -1);
    Vector2Int screenSize = new Vector2Int(-1, -1);

    void OnEnable()
    {
        rectTransform = (RectTransform)transform;
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != currentSafeArea ||
            Screen.width != screenSize.x ||
            Screen.height != screenSize.y)
        {
            Apply();
        }
    }

    void Apply()
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        var safeArea = Screen.safeArea;
        int w = Screen.width, h = Screen.height;
        currentSafeArea = safeArea;
        screenSize = new Vector2Int(w, h);
        if (w <= 0 || h <= 0) return;
        rectTransform.anchorMin = new Vector2(safeArea.xMin / w, safeArea.yMin / h);
        rectTransform.anchorMax = new Vector2(safeArea.xMax / w, safeArea.yMax / h);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
