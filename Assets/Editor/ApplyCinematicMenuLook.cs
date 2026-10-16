using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;

public static class ApplyCinematicMenuLook
{
    [MenuItem("Tools/Build MainMenu Background")]
    public static void Apply()
    {
        try
        {
            ApplyToActiveScene();
            SaveActiveScene();
            Debug.Log("[MainMenu] Background built: sea + sky + cinematic look. Scene saved.");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[MainMenu] Build FAILED - see exception above.");
        }
    }

    /// <summary>
    /// Builds the cinematic ocean backdrop (sea, camera, sky, fog, sun, post
    /// processing) in whatever scene is currently active, without saving.
    /// Shared by the About scene builder so both scenes look identical.
    /// Requires a Main Camera (tagged) and a "Directional Light" to exist.
    /// </summary>
    public static void ApplyToActiveScene()
    {

        // --- cleanup from previous runs ---
        DestroyIfExists("Raft");
        DestroyIfExists("Islands");
        DestroyIfExists("Ground");

        // --- Sea ---
        var sea = GameObject.Find("Sea");
        if (sea == null)
        {
            sea = new GameObject("Sea");
            sea.AddComponent<MeshFilter>();
            sea.AddComponent<MeshRenderer>();
            sea.AddComponent<LowPolySea>();
        }
        sea.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        sea.transform.localScale = Vector3.one;
        var seaComp = sea.GetComponent<LowPolySea>();
        seaComp.resolution = 80;
        seaComp.size = 220;
        seaComp.waveHeight = 1.2f;
        seaComp.waveFrequency = 0.5f;
        seaComp.waveSpeed = 1.1f;
        seaComp.sharpness = 1.3f;
        EditorUtility.SetDirty(seaComp);
        typeof(LowPolySea).GetMethod("BuildMesh", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(seaComp, null);
        var seaMat = LitMat("Assets/Settings/Sea.mat",
            new Color(15f / 255f, 70f / 255f, 95f / 255f), 0.3f,
            new Color(10f / 255f, 40f / 255f, 55f / 255f));
        sea.GetComponent<MeshRenderer>().sharedMaterial = seaMat;

        // --- Camera ---
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 2.8f, -13f);
            cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            cam.fieldOfView = 55f;
            if (cam.GetComponent<CameraSway>() == null) cam.gameObject.AddComponent<CameraSway>();
            var urpCam = cam.GetUniversalAdditionalCameraData();
            urpCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            urpCam.antialiasingQuality = AntialiasingQuality.High;
            EditorUtility.SetDirty(urpCam);
        }

        // --- MSAA on the URP assets (cleans up the low-poly horizon edge) ---
        foreach (var rpPath in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
            if (rp != null)
            {
                rp.msaaSampleCount = 4;
                EditorUtility.SetDirty(rp);
            }
        }

        // --- Sky (Gradient shader was removed in Unity 6, use tuned Procedural) ---
        var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Sky.mat");
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, "Assets/Settings/Sky.mat");
        }
        sky.SetFloat("_SunSize", 0.045f);
        sky.SetFloat("_SunSizeConvergence", 1.3f);
        sky.SetFloat("_AtmosphereThickness", 1.45f);
        sky.SetColor("_SkyTint", new Color(0.48f, 0.32f, 0.62f));
        sky.SetColor("_GroundColor", new Color(50f / 255f, 30f / 255f, 50f / 255f));
        sky.SetFloat("_Exposure", 1.15f);
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;

        // --- Fog + ambient ---
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(230f / 255f, 140f / 255f, 110f / 255f);
        RenderSettings.fogDensity = 0.015f;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;

        // --- Sun ---
        var sunGO = GameObject.Find("Directional Light");
        if (sunGO != null)
        {
            var sun = sunGO.GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            sun.useColorTemperature = false;
            sun.color = new Color(1f, 170f / 255f, 120f / 255f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            EditorUtility.SetDirty(sun);
        }

        // --- Post processing ---
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        if (profile != null)
        {
            if (!profile.TryGet<Tonemapping>(out var tm)) tm = profile.Add<Tonemapping>(true);
            tm.active = true;
            tm.mode.overrideState = true;
            tm.mode.value = TonemappingMode.ACES;

            if (!profile.TryGet<ColorAdjustments>(out var ca)) ca = profile.Add<ColorAdjustments>(true);
            ca.active = true;
            ca.postExposure.overrideState = true; ca.postExposure.value = 0.15f;
            ca.saturation.overrideState = true; ca.saturation.value = 15f;
            ca.contrast.overrideState = true; ca.contrast.value = 10f;
            ca.colorFilter.overrideState = true;
            ca.colorFilter.value = new Color(1f, 220f / 255f, 200f / 255f);

            if (!profile.TryGet<Bloom>(out var bl)) bl = profile.Add<Bloom>(true);
            bl.active = true;
            bl.intensity.overrideState = true; bl.intensity.value = 0.4f;
            bl.threshold.overrideState = true; bl.threshold.value = 0.9f;

            if (!profile.TryGet<Vignette>(out var vg)) vg = profile.Add<Vignette>(true);
            vg.active = true;
            vg.intensity.overrideState = true; vg.intensity.value = 0.25f;
            vg.smoothness.overrideState = true; vg.smoothness.value = 0.4f;

            EditorUtility.SetDirty(profile);
        }
    }

    static void SaveActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static Material LitMat(string path, Color color, float smoothness, Color? emission = null)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
