using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ControlsHUD : MonoBehaviour
{
    [System.Serializable]
    public class ControlEntry
    {
        public string key = "E";
        [TextArea(1,2)]
        public string description = "Detach";
        public bool visible = true;
    }

    [Header("Entries (editable in Inspector)")]
    public List<ControlEntry> entries = new List<ControlEntry>
    {
        new ControlEntry { key = "E", description = "Detach / Interact" },
    };

    [Header("Appearance")]
    public int fontSize = 14;
    public Vector2 panelSize = new Vector2(220, 120);
    public Vector2 margin = new Vector2(12, 12);
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    public Color textColor = Color.white;
    public Font font;

    [Header("Behavior")]
    public bool createIfNoCanvas = true;
    public bool dontDestroyOnLoad = false;

    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform panel;
    List<Text> itemTexts = new List<Text>();

    Rect lastSafeArea = new Rect(0,0,0,0);

    void Awake()
    {
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (rootCanvas == null)
            rootCanvas = FindObjectOfType<Canvas>();

        if (rootCanvas == null && createIfNoCanvas && Application.isPlaying)
            rootCanvas = CreateDefaultCanvas();

        if (rootCanvas == null)
        {
            Debug.LogWarning("[ControlsHUD] No Canvas found. Assign a Canvas in the inspector or enable Create If No Canvas and run the game.");
            return;
        }

        if (panel == null && Application.isPlaying)
            CreatePanel();

        if (Application.isPlaying)
        {
            BuildItems();
            ApplySafeArea();

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
    }

    void Update()
    {
        if (panel == null || rootCanvas == null) return;
        Rect safe = Screen.safeArea;
        if (safe != lastSafeArea || Screen.width != (int)lastSafeArea.width || Screen.height != (int)lastSafeArea.height)
            ApplySafeArea();
    }

    Canvas CreateDefaultCanvas()
    {
        GameObject cgo = new GameObject("ControlsHUD_Canvas");
        var c = cgo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cgo.AddComponent<GraphicRaycaster>();
        return c;
    }

    void CreatePanel()
    {
        if (!Application.isPlaying) return; 
        if (panel != null) return;

        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
            if (rootCanvas == null && createIfNoCanvas)
                rootCanvas = CreateDefaultCanvas();
        }

        if (rootCanvas == null)
        {
            Debug.LogWarning("[ControlsHUD] CreatePanel aborted: no Canvas available.");
            return;
        }

        GameObject panelGO = new GameObject("ControlsHUD_Panel");
        panelGO.transform.SetParent(rootCanvas.transform, false);

        panel = panelGO.AddComponent<RectTransform>();
        var image = panelGO.AddComponent<Image>();
        image.color = backgroundColor;

        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.sizeDelta = panelSize;
        panel.anchoredPosition = new Vector2(margin.x, margin.y);

        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4f;
        var fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void BuildItems()
    {
        if (!Application.isPlaying) return;

        foreach (var t in itemTexts)
            if (t != null) Destroy(t.gameObject);
        itemTexts.Clear();

        if (panel == null)
        {
            CreatePanel();
            if (panel == null) return;
        }

        foreach (var e in entries)
        {
            if (!e.visible) continue;

            GameObject line = new GameObject("ctrl_" + e.key);
            line.transform.SetParent(panel, false);

            var rt = line.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(panelSize.x - 20f, 0f);

            var txt = line.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.supportRichText = true;

            txt.text = $"<b>{e.key}</b>  —  {e.description}";

            itemTexts.Add(txt);
        }
    }

    public void Refresh()
    {
        if (!Application.isPlaying)
        {

            if (panel != null && rootCanvas != null)
                ApplySafeArea();
            return;
        }

        if (panel == null) CreatePanel();
        BuildItems();
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        if (panel == null || rootCanvas == null) return;

        Rect safe = Screen.safeArea;
        lastSafeArea = safe;

        float scale = 1f;
        var cs = rootCanvas.GetComponent<CanvasScaler>();
        scale = rootCanvas.scaleFactor;

        float safeX = safe.xMin / scale;
        float safeY = safe.yMin / scale;

        panel.anchoredPosition = new Vector2(margin.x + safeX, margin.y + safeY);

        float safeWidthPx = safe.width / scale;
        float targetWidth = Mathf.Min(panelSize.x, safeWidthPx - margin.x * 2f);
        panel.sizeDelta = new Vector2(targetWidth, panel.sizeDelta.y);
    }

#if UNITY_EDITOR
    void OnValidate()
    {

        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
#endif
}
