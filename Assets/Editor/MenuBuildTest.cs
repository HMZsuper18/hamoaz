using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MenuBuildTest
{
    static string FullPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return FullPath(t.parent) + "/" + t.name;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        MainMenuUIBuilder.Build();

        var canvasGo = GameObject.Find("MainMenuCanvas");
        var cam = Camera.main;
        if (canvasGo == null || cam == null)
        {
            Debug.LogError("[TEST] Missing canvas or camera");
            EditorApplication.Exit(1);
            return;
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        var canvasRt = (RectTransform)canvasGo.transform;
        Debug.Log("[TEST] canvas rect=" + canvasRt.rect + " scaleFactor=" + canvas.scaleFactor
                  + " screen=" + Screen.width + "x" + Screen.height);

        var panel = canvasGo.transform.Find("SafeArea/MenuPanel") as RectTransform;
        Debug.Log("[TEST] panel rect=" + (panel != null ? panel.rect.ToString() : "null"));

        foreach (Transform child in panel)
        {
            var r = (RectTransform)child;
            var sb = new StringBuilder();
            sb.Append("[TEST] ").Append(child.name)
              .Append(" rect=").Append(r.rect)
              .Append(" sizeDelta=").Append(r.sizeDelta)
              .Append(" anchors=").Append(r.anchorMin).Append("..").Append(r.anchorMax)
              .Append(" scale=").Append(r.localScale);
            Debug.Log(sb.ToString());

            foreach (var c in child.GetComponents<Component>())
                Debug.Log("[TEST]   comp: " + c.GetType().Name + (c is Behaviour b ? (b.enabled ? " (on)" : " (OFF)") : ""));

            foreach (var le in child.GetComponents<LayoutElement>())
                Debug.Log("[TEST]   LE pref=" + le.preferredWidth + "x" + le.preferredHeight
                          + " min=" + le.minWidth + "x" + le.minHeight
                          + " flex=" + le.flexibleWidth + "x" + le.flexibleHeight
                          + " ignore=" + le.ignoreLayout + " prio=" + le.layoutPriority);

            Debug.Log("[TEST]   LayoutUtility pref=" + LayoutUtility.GetPreferredWidth(r)
                      + "x" + LayoutUtility.GetPreferredHeight(r)
                      + " min=" + LayoutUtility.GetMinWidth(r) + "x" + LayoutUtility.GetMinHeight(r));

            foreach (var lg in panel.GetComponents<LayoutGroup>())
                Debug.Log("[TEST]   on-panel group: " + lg.GetType().Name);
        }

        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        Debug.Log("[TEST] VLG control=" + vlg.childControlWidth + "/" + vlg.childControlHeight
                  + " force=" + vlg.childForceExpandWidth + "/" + vlg.childForceExpandHeight
                  + " spacing=" + vlg.spacing);

        Debug.Log("[TEST-AFTER] panel rect=" + panel.rect);
        foreach (Transform child in panel)
        {
            var r = (RectTransform)child;
            Debug.Log("[TEST-AFTER] " + child.name + " rect=" + r.rect + " sizeDelta=" + r.sizeDelta);

            var all = child.GetComponents<Component>();
            foreach (var c in all)
            {
                if (c is UnityEngine.UI.ILayoutElement ile)
                {
                    Debug.Log("[TEST-AFTER]   ILayoutElement: " + c.GetType().Name
                        + " prio=" + ile.layoutPriority
                        + " min=" + ile.minWidth + "x" + ile.minHeight
                        + " pref=" + ile.preferredWidth + "x" + ile.preferredHeight
                        + " max=" + ile.maxWidth + "x" + ile.maxHeight
                        + " flex=" + ile.flexibleWidth + "x" + ile.flexibleHeight
                        + (c is Behaviour b2 ? (b2.isActiveAndEnabled ? " (on)" : " (OFF)") : ""));
                }
                else
                {
                    Debug.Log("[TEST-AFTER]   comp: " + c.GetType().Name);
                }
            }

            var srcP = LayoutUtility.GetLayoutProperty(r, e => e.preferredHeight, 0, out var srcPC);
            var srcM = LayoutUtility.GetLayoutProperty(r, e => e.minHeight, 0, out var srcMC);
            var srcX = LayoutUtility.GetLayoutProperty(r, e => e.maxHeight, 0, out var srcXC);
            Debug.Log("[TEST-AFTER]   GetLayoutProperty prefH=" + srcP + " src=" + (srcPC != null ? srcPC.GetType().Name : "none")
                + " minH=" + srcM + " src=" + (srcMC != null ? srcMC.GetType().Name : "none")
                + " maxH=" + srcX + " src=" + (srcXC != null ? srcXC.GetType().Name : "none")
                + " | GetPreferredHeight=" + LayoutUtility.GetPreferredHeight(r));

            foreach (Transform sub in child)
            {
                var sr = (RectTransform)sub;
                Debug.Log("[TEST-AFTER]     child " + sub.name + " rect=" + sr.rect + " sizeDelta=" + sr.sizeDelta);
            }
        }

        // Batchmode leaves Screen at 640x480 and a ScreenSpaceOverlay canvas draws in
        // its own pass after cameras, so neither Overlay nor a default camera render
        // can be captured. Instead: park a camera in front of a ScreenSpaceCamera
        // canvas whose planeDistance sits between near and far, and drive its pixel
        // size directly from the RT.
        //
        // The CanvasScaler is disabled here (Screen is stuck at 640x480 in batchmode),
        // so each capture simulates ScaleWithScreenSize/Expand by hand: scale =
        // min(targetW/referenceW, targetH/referenceH), which is exactly what Expand
        // does — the 16:9 design area stays locked and letterboxes on other aspects.
        const float refW = 1920f;
        const float refH = 1080f;
        CaptureAt(canvasGo, canvasRt, 1920, 1080, "/tmp/opencode/menu_test.png", refW, refH);
        CaptureAt(canvasGo, canvasRt, 2560, 1080, "/tmp/opencode/menu_test_ultrawide.png", refW, refH);
        CaptureAt(canvasGo, canvasRt, 1280, 720, "/tmp/opencode/menu_test_720p.png", refW, refH);
        CaptureAt(canvasGo, canvasRt, 1024, 768, "/tmp/opencode/menu_test_4_3.png", refW, refH);

        // Restore the real runtime configuration before saving so the editor Game
        // view uses the adaptive CanvasScaler path, not the capture rig.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        var scaler2 = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler2 != null)
        {
            scaler2.enabled = true;
            scaler2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler2.referenceResolution = new Vector2(refW, refH);
            scaler2.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;
        canvasRt.sizeDelta = Vector2.zero;
        canvasRt.localScale = Vector3.one;

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[TEST] screenshots saved OK");
        EditorApplication.Exit(0);
    }

    public static void CaptureAt(GameObject canvasGo, RectTransform canvasRt, int w, int h, string path, float refW, float refH)
    {
        var panel = canvasGo.transform.Find("SafeArea/MenuPanel") as RectTransform;
        var rt = new RenderTexture(w, h, 24);

        var camGo = new GameObject("__CaptureCam");
        var capCam = camGo.AddComponent<Camera>();
        capCam.clearFlags = CameraClearFlags.SolidColor;
        capCam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        capCam.orthographic = false;
        capCam.nearClipPlane = 0.01f;
        capCam.farClipPlane = 100f;
        capCam.targetTexture = rt;
        capCam.transform.position = new Vector3(0f, 0f, -10f);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = capCam;
        canvas.planeDistance = 20f;

        // Emulate ScaleWithScreenSize/Expand exactly: give the scaler a
        // ConstantPixelSize factor equal to Expand's min(ratio). Unity then sizes the
        // root canvas rect itself on render (pixelRect/factor) — no manual rect
        // fighting, which previously broke when scale != 1.
        float scale = Mathf.Min(w / refW, h / refH);
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler != null)
        {
            scaler.enabled = true;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;
        }

        Canvas.ForceUpdateCanvases();
        capCam.Render(); // let Unity settle the ScreenSpaceCamera canvas rect

        Canvas.ForceUpdateCanvases();
        foreach (var fit in canvasGo.GetComponentsInChildren<FlushLeftHeightScale>(true))
            fit.ForceApply();
        foreach (var fit in canvasGo.GetComponentsInChildren<ContainFitScale>(true))
            fit.ForceApply();
        Canvas.ForceUpdateCanvases();
        if (panel != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        Canvas.ForceUpdateCanvases();

        // Geometry diagnostics for resolution-locked layout debugging.
        {
            var safe = canvasGo.transform.Find("SafeArea") as RectTransform;
            Debug.Log("[CAP] " + w + "x" + h + " scale=" + scale
                + " screen=" + Screen.width + "x" + Screen.height + " safeArea=" + Screen.safeArea
                + " canvas.rect=" + canvasRt.rect + " canvas.pos=" + canvasRt.position
                + " canvas.anchoredPos=" + canvasRt.anchoredPosition);
            if (safe != null)
                Debug.Log("[CAP]   safe anchors=" + safe.anchorMin + ".." + safe.anchorMax
                    + " offsetMin=" + safe.offsetMin + " offsetMax=" + safe.offsetMax
                    + " rect=" + safe.rect);
            if (panel != null)
                Debug.Log("[CAP]   panel anchoredPos=" + panel.anchoredPosition
                    + " localScale=" + panel.localScale
                    + " rect=" + panel.rect + " worldX=" + panel.TransformPoint(panel.rect.center).x
                    + " canvas.scaleFactor=" + canvas.scaleFactor);
        }

        capCam.Render();
        Debug.Log("[CAP]   afterRender canvas.rect=" + canvasRt.rect
            + " canvas.pos=" + canvasRt.position
            + " canvas.anchoredPos=" + canvasRt.anchoredPosition
            + " localScale=" + canvasRt.localScale);

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;
        capCam.targetTexture = null;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Debug.Log("[TEST] screenshot saved " + w + "x" + h + " scale=" + scale + " canvas=" + (w / scale) + "x" + (h / scale) + " -> " + path);
    }
}
