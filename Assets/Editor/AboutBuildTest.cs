using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Batch/CI harness for the About scene: builds it via AboutUIBuilder,
/// captures a 1920x1080 screenshot, then exits.
/// Usage: Unity -batchmode -projectPath . -executeMethod AboutBuildTest.Run
/// </summary>
public static class AboutBuildTest
{
    public static void Run()
    {
        if (!File.Exists("Assets/Scenes/About.unity"))
        {
            // First run: create the scene file before Build() opens it.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/About.unity");
        }
        EditorSceneManager.OpenScene("Assets/Scenes/About.unity");
        AboutUIBuilder.Build();

        var canvasGo = GameObject.Find("AboutCanvas");
        var cam = Camera.main;
        if (canvasGo == null || cam == null)
        {
            Debug.LogError("[TEST] Missing AboutCanvas or camera");
            EditorApplication.Exit(1);
            return;
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        var canvasRt = (RectTransform)canvasGo.transform;
        Debug.Log("[TEST] canvas rect=" + canvasRt.rect + " scaleFactor=" + canvas.scaleFactor);

        foreach (Transform child in canvasGo.transform.Find("SafeArea/AboutPanel"))
            Debug.Log("[TEST] panel child " + child.name + " rect=" + ((RectTransform)child).rect);

        const float refW = 1920f;
        const float refH = 1080f;
        MenuBuildTest.CaptureAt(canvasGo, canvasRt, 1920, 1080, "/tmp/opencode/about_test.png", refW, refH);
        MenuBuildTest.CaptureAt(canvasGo, canvasRt, 2560, 1080, "/tmp/opencode/about_test_ultrawide.png", refW, refH);
        MenuBuildTest.CaptureAt(canvasGo, canvasRt, 1280, 720, "/tmp/opencode/about_test_720p.png", refW, refH);
        MenuBuildTest.CaptureAt(canvasGo, canvasRt, 1024, 768, "/tmp/opencode/about_test_4_3.png", refW, refH);
        MenuBuildTest.CaptureAt(canvasGo, canvasRt, 720, 1280, "/tmp/opencode/about_test_portrait.png", refW, refH);
        MenuBuildTest.CaptureAt(canvasGo, canvasRt, 3840, 2160, "/tmp/opencode/about_test_4k.png", refW, refH);

        // Restore the real runtime configuration before saving.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.enabled = true;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(refW, refH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;
        canvasRt.sizeDelta = Vector2.zero;
        canvasRt.localScale = Vector3.one;

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[TEST] About scene screenshot saved OK");
        EditorApplication.Exit(0);
    }
}
