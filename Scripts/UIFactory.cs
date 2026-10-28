using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Builds uGUI elements from code so scenes need no hand-authored UI.</summary>
public static class UIFactory
{
    static Font font;

    public static Font GetFont()
    {
        if (font == null)
        {
#if UNITY_2022_2_OR_NEWER
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
        return font;
    }

    public static Canvas CreateCanvas(string name, int sortingOrder = 0)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    public static RectTransform Panel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Image img = go.GetComponent<Image>();
        img.color = color;
        return rt;
    }

    public static RectTransform FullScreenPanel(Transform parent, string name, Color color)
    {
        return Panel(parent, name, color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    public static Text Label(Transform parent, string name, string text, int size, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Text t = go.GetComponent<Text>();
        t.font = GetFont();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Text centred at an anchored position (centre-anchored).</summary>
    public static Text CenterLabel(Transform parent, string text, int size, Vector2 pos, Vector2 boxSize, Color color)
    {
        Text t = Label(parent, "Label", text, size, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, color);
        RectTransform rt = t.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = boxSize;
        return t;
    }

    public static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityAction onClick)
    {
        GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.16f, 0.2f, 0.32f, 0.95f);

        Button b = go.GetComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.55f, 0.85f, 1f, 1f);
        cb.pressedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
        b.colors = cb;
        b.targetGraphic = img;
        if (onClick != null) b.onClick.AddListener(onClick);

        Text t = Label(go.transform, "Text", label, 34, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        t.raycastTarget = false;
        return b;
    }
}
