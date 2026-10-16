using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public static class MainMenuUIBuilder
{
    static TMP_FontAsset s_titleFont;

    [MenuItem("Tools/Build Main Menu Buttons")]
    public static void Build()
    {
        try { BuildInternal(); }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[MainMenuUI] Build FAILED - see exception above.");
        }
    }

    static void BuildInternal()
    {
        foreach (var p in new[]
        {
            "Assets/UI/WoodFrame.png", "Assets/UI/PanelFill.png",
            "Assets/UI/Fonts/Fredoka-Bold SDF.asset", "Assets/UI/Fonts/Fredoka-Bold.ttf",
            "Assets/UI/Fonts/Fredoka-SemiBold SDF.asset", "Assets/UI/Fonts/Fredoka-SemiBold.ttf",
        })
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p) != null) AssetDatabase.DeleteAsset(p);

        s_titleFont = LoadTitleFont();
        Debug.Log("[MainMenuUI] title font = " + (s_titleFont != null ? s_titleFont.name : "LiberationSans (fallback)"));

        // Brush-strip silhouette extracted from the reference screenshot (Python step
        // writes these two PNGs); here we only import them as sprites.
        var brush = LoadSprite("Assets/UI/BrushStrip.png");
        var outline = LoadSprite("Assets/UI/BrushStripOutline.png");
        var icons = new[]
        {
            LoadSprite("Assets/UI/Icons/IconPlay.png"),
            LoadSprite("Assets/UI/Icons/IconMultiplayer.png"),
            LoadSprite("Assets/UI/Icons/IconSettings.png"),
            LoadSprite("Assets/UI/Icons/IconAbout.png"),
        };
        AssetDatabase.SaveAssets();

        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MainMenuButton.prefab") != null)
            AssetDatabase.DeleteAsset("Assets/Prefabs/MainMenuButton.prefab");
        Directory.CreateDirectory("Assets/Prefabs");
        var template = BuildTemplate(brush, outline);
        var prefab = PrefabUtility.SaveAsPrefabAsset(template, "Assets/Prefabs/MainMenuButton.prefab");
        UnityEngine.Object.DestroyImmediate(template);

        BuildScene(prefab, icons);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[MainMenuUI] Built 4 main menu buttons (Singleplayer, Multiplayer, Settings, About). Scene saved.");
    }

    public static TMP_FontAsset LoadTitleFont()
    {
        const string ttf = "Assets/UI/Fonts/Fredoka-Medium.ttf";
        const string asset = "Assets/UI/Fonts/Fredoka-Medium SDF.asset";
        try
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(asset);
            if (existing != null) return existing;
            if (!File.Exists(ttf)) return null;
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            if (font == null)
            {
                AssetDatabase.ImportAsset(ttf, ImportAssetOptions.ForceUpdate);
                font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            }
            if (font == null) return null;
            var fa = TMP_FontAsset.CreateFontAsset(font, 64, 8,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDF, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            if (fa == null) return null;
            AssetDatabase.CreateAsset(fa, asset);
            if (fa.material != null) AssetDatabase.AddObjectToAsset(fa.material, fa);
            if (fa.atlasTexture != null) AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MainMenuUI] Fredoka TMP asset failed: " + e.Message);
            return null;
        }
    }

    // ---------- scene ----------

    static void BuildScene(GameObject prefab, Sprite[] icons)
    {
        var existing = GameObject.Find("MainMenuCanvas");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        var canvasGo = new GameObject("MainMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<GraphicRaycaster>();
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // Expand + the FlushLeftHeightScale / SafeAreaFitter children give a
        // stable design space: the canvas root stretches (Expand), SafeAreaFitter
        // crops to the safe area, and MenuPanel is pinned flush to the real
        // screen-left edge with size scaled by screen height - identical menu
        // geometry on every resolution and aspect.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var safeGo = new GameObject("SafeArea");
        var safeRt = safeGo.AddComponent<RectTransform>();
        safeRt.SetParent(canvasGo.transform, false);
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = Vector2.zero;
        safeRt.offsetMax = Vector2.zero;
        safeGo.AddComponent<SafeAreaFitter>();

        var panelGo = new GameObject("MenuPanel");
        var panelRt = panelGo.AddComponent<RectTransform>();
        // Parented directly to the SafeArea and pinned flush to the TRUE screen
        // left by FlushLeftHeightScale (scales by screen height vs. a 1080p
        // reference), so the buttons hug the physical left edge at every
        // resolution and aspect instead of riding inside a centered 16:9 box.
        panelRt.SetParent(safeGo.transform, false);
        var flush = panelGo.AddComponent<FlushLeftHeightScale>();
        // Small scaled inset: flush-at-0 keeps the torn brush edge clipped at the
        // physical screen edge (reads as "outside the screen", esp. with overscan),
        // so we back off ~1.7% of the design width instead.
        flush.leftMargin = 32f;
        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 22f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        var csf = panelGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var labels = new[] { "Singleplayer", "Multiplayer", "Settings", "About" };
        // Scene/URL wired per button via SceneButton (null = not implemented yet).
        var navScenes = new string[] { null, null, null, "About" };
        for (int i = 0; i < 4; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, panelRt.transform);
            go.name = labels[i] + "Button";
            // Unity 6: Image implements ILayoutElement (pref = sprite size) and HLG contributes
            // min/max on the same root; pin min/pref/max so the root size is exactly ours.
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 372f; le.preferredWidth = 372f; le.maxWidth = 372f;
            le.minHeight = 68f; le.preferredHeight = 68f; le.maxHeight = 68f;
            // Explicit 0 (not the -1 default) so a parent layout group never
            // stretches the button: the prefab's inner HLG reports flexibleWidth
            // = 1, and -1 is ignored by LayoutUtility.
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
            ConfigureButton(go, labels[i], icons[i], i == 0);
            if (navScenes[i] != null)
            {
                var nav = go.AddComponent<SceneButton>();
                nav.sceneName = navScenes[i];
            }
        }

        if (GameObject.Find("EventSystem") == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions != null) module.actionsAsset = actions;
        }
    }

    public static void ConfigureButton(GameObject go, string label, Sprite icon, bool primary)
    {
        if (s_titleFont == null) s_titleFont = LoadTitleFont();

        var img = go.GetComponent<Image>();
        var outlineImg = go.transform.Find("Outline").GetComponent<Image>();
        var iconImg = go.transform.Find("Icon").GetComponent<Image>();
        var labelTmp = go.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        labelTmp.text = label;
        if (s_titleFont != null)
        {
            labelTmp.font = s_titleFont;
            labelTmp.fontStyle = FontStyles.Normal;
        }
        else
        {
            labelTmp.fontStyle = FontStyles.Bold;
        }
        // Medium reads much lighter than SemiBold at the same size; the reference
        // letterforms are delicate, so no synthetic bolding. Cap-height ratio ~0.39
        // of the 88px button.
        labelTmp.fontSize = 22f;
        labelTmp.color = Color.white;

        iconImg.sprite = icon;
        iconImg.color = new Color(1f, 1f, 1f, 0.92f);

        // Constant layout box for every icon so their left edges (and the text start)
        // line up across all four buttons. Per-icon visual size is baked into the
        // sprite's transparent padding instead of the layout box.
        // Sized to the reference: icon height ~0.60 of button height.
        const float iconBox = 40f;
        var iconRt = iconImg.rectTransform;
        var iconLe = iconRt.GetComponent<LayoutElement>();
        if (iconLe != null)
        {
            iconLe.preferredWidth = iconBox;
            iconLe.preferredHeight = iconBox;
        }
        iconRt.sizeDelta = new Vector2(iconBox, iconBox);

        // Source colors pre-compensated for the camera's tone mapping/vignette so the
        // FINAL on-screen pixels match the reference (teal #0698AC, slate #153B4E).
        img.color = primary ? Hex("#22B8D3") : Hex("#375F73");
        // Rim only a touch brighter than the body — the reference has a soft highlight,
        // not a drawn outline. The texture itself already fades the rim to ~50% alpha.
        outlineImg.color = primary ? Hex("#2FCCDE") : Hex("#4E7185");
    }

    public static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }

    // ---------- prefab ----------

    static GameObject BuildTemplate(Sprite brush, Sprite outline)
    {
        var root = new GameObject("MainMenuButton");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(372f, 68f);
        var rootImg = root.AddComponent<Image>();
        rootImg.sprite = brush;
        rootImg.type = Image.Type.Simple;
        rootImg.color = Color.white;
        rootImg.raycastTarget = true;
        root.AddComponent<CanvasGroup>();
        var shadow = root.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
        shadow.effectDistance = new Vector2(0f, -4f);
        shadow.useGraphicAlpha = true;

        var button = root.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = rootImg;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(48, 40, 0, 0);
        hlg.spacing = 29f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // light rim that traces the torn shape, rendered above the body but below icon/label.
        var outlineGo = new GameObject("Outline", typeof(RectTransform));
        outlineGo.transform.SetParent(rootRt, false);
        var outlineRt = outlineGo.GetComponent<RectTransform>();
        outlineRt.anchorMin = Vector2.zero;
        outlineRt.anchorMax = Vector2.one;
        outlineRt.offsetMin = Vector2.zero;
        outlineRt.offsetMax = Vector2.zero;
        var outlineImg = outlineGo.AddComponent<Image>();
        outlineImg.sprite = outline;
        outlineImg.type = Image.Type.Simple;
        outlineImg.color = Color.white;
        outlineImg.raycastTarget = false;
        outlineGo.AddComponent<LayoutElement>().ignoreLayout = true;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(rootRt, false);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = new Color(1f, 1f, 1f, 0.92f);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        var iconLe = iconGo.AddComponent<LayoutElement>();
        iconLe.preferredWidth = 40f;
        iconLe.preferredHeight = 40f;
        iconLe.flexibleWidth = 0f;
        ((RectTransform)iconGo.transform).sizeDelta = new Vector2(40f, 40f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rootRt, false);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "Label";
        label.fontSize = 22f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        ((RectTransform)labelGo.transform).sizeDelta = new Vector2(100f, 46f);

        var feedback = root.AddComponent<MenuButtonFeedback>();
        feedback.accentBar = null;
        return root;
    }

    // ---------- assets ----------

    static Sprite LoadSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

}
