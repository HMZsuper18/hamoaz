using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Click action for code-generated menu buttons: loads a scene by name, or
/// opens an external URL (URL wins when both are set). The fields are wired by
/// the editor builders; the listener is attached in Awake so it survives
/// scene serialization without persistent UnityEvent calls.
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneButton : MonoBehaviour
{
    [Tooltip("Scene name (must be in Build Settings) loaded on click. Leave empty to do nothing.")]
    public string sceneName;

    [Tooltip("External URL opened on click. Takes precedence over sceneName.")]
    public string url;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
            return;
        }

        if (string.IsNullOrEmpty(sceneName)) return;

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("[SceneButton] Scene not in build settings: \"" + sceneName + "\" (button \"" + name + "\").");
        }
    }
}
