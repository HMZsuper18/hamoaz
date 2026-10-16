using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.04f;
    public float pressedScale = 0.96f;
    public float lerpSpeed = 14f;
    public Graphic accentBar;

    Button button;
    CanvasGroup group;
    Vector3 baseScale;
    bool hovered;
    bool pressed;
    bool lastInteractable = true;

    void Awake()
    {
        button = GetComponent<Button>();
        group = GetComponent<CanvasGroup>();
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (accentBar != null)
        {
            var c = accentBar.color;
            accentBar.color = new Color(c.r, c.g, c.b, 0f);
        }
        lastInteractable = button != null ? button.interactable : true;
        ApplyInteractable();
    }

    void Update()
    {
        if (button != null && button.interactable != lastInteractable)
        {
            lastInteractable = button.interactable;
            ApplyInteractable();
        }

        float target = hovered ? (pressed ? pressedScale : hoverScale) : 1f;
        if (!hovered && pressed) target = pressedScale;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            baseScale * target,
            Time.unscaledDeltaTime * lerpSpeed);

        bool selected = EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == gameObject;
        if (accentBar != null)
        {
            bool on = (hovered || selected) && button != null && button.interactable;
            float a = on ? 1f : 0f;
            var c = accentBar.color;
            accentBar.color = new Color(c.r, c.g, c.b,
                Mathf.MoveTowards(c.a, a, Time.unscaledDeltaTime * 6f));
        }
    }

    void ApplyInteractable()
    {
        if (group != null)
            group.alpha = button != null && button.interactable ? 1f : 0.55f;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || button.interactable) pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData) => pressed = false;
}
