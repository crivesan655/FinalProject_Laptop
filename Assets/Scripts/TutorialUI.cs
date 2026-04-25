using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance { get; private set; }

    [Header("UI References (optional, created at runtime if null)")]
    public Canvas canvas;
    public RectTransform panel;
    public Text titleText;
    public Text descriptionText;
    public Text hintText;

    [Header("Adaptive sizing")]

    public float minPanelWidth = 320f;
    public float maxPanelWidth = 800f;
    public float minPanelHeight = 80f;
    public float maxPanelHeight = 600f;

    [Header("Placement")]
    public float topPadding = 24f; 

    [Header("Highlight")]
    public Vector2 highlightSize = new Vector2(48, 48);
    public Color highlightColor = new Color(1f, 0.85f, 0.2f, 0.95f);

    bool isVisible = false;
    TutorialStep currentStep;

    RectTransform highlightMarker;
    public event Action OnNextPressed;

    Coroutine layoutCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (canvas == null)
        {
            GameObject c = new GameObject("TutorialCanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = c.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            c.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(c);
        }

        if (panel == null)
            CreateDefaultPanel();

        EnsurePanelSetup();

        EnsureHighlightMarker();
    }

    void CreateDefaultPanel()
    {
        GameObject p = new GameObject("TutorialPanel");
        p.transform.SetParent(canvas.transform, false);

        panel = p.AddComponent<RectTransform>();
        panel.localScale = Vector3.one;

        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);

        panel.sizeDelta = new Vector2(minPanelWidth, minPanelHeight);
        panel.anchoredPosition = new Vector2(0f, -topPadding);

        var image = p.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);
    }

    void EnsurePanelSetup()
    {
        if (panel == null) return;

        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.localScale = Vector3.one;

        var img = panel.GetComponent<Image>();
        if (img == null)
        {
            img = panel.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
        }

        // VerticalLayoutGroup
        var vlayout = panel.GetComponent<VerticalLayoutGroup>();
        if (vlayout == null)
        {
            vlayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlayout.childControlHeight = true;
            vlayout.childControlWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.childForceExpandWidth = true;
            vlayout.spacing = 6f;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            vlayout.childAlignment = TextAnchor.UpperLeft;
        }

        Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Title
        if (titleText == null)
        {
            var t = panel.Find("Title");
            if (t != null) titleText = t.GetComponent<Text>();
        }
        if (titleText == null)
        {
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel, false);
            titleText = titleGO.AddComponent<Text>();
        }
        // ensure title Text settings
        titleText.font = titleText.font ?? runtimeFont;
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.color = Color.white;
        titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        var titleLayout = titleText.GetComponent<LayoutElement>() ?? titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 32f;
        titleLayout.flexibleHeight = 0f;

        // Description
        if (descriptionText == null)
        {
            var d = panel.Find("Description");
            if (d != null) descriptionText = d.GetComponent<Text>();
        }
        if (descriptionText == null)
        {
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(panel, false);
            descriptionText = descGO.AddComponent<Text>();
        }
        descriptionText.font = descriptionText.font ?? runtimeFont;
        descriptionText.fontSize = 14;
        descriptionText.alignment = TextAnchor.UpperLeft;
        descriptionText.color = Color.white;
        descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
        var descLayout = descriptionText.GetComponent<LayoutElement>() ?? descriptionText.gameObject.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 0f;
        descLayout.flexibleHeight = 1f;

        // Hint
        if (hintText == null)
        {
            var h = panel.Find("Hint");
            if (h != null) hintText = h.GetComponent<Text>();
        }
        if (hintText == null)
        {
            var hintGO = new GameObject("Hint");
            hintGO.transform.SetParent(panel, false);
            hintText = hintGO.AddComponent<Text>();
        }
        hintText.font = hintText.font ?? runtimeFont;
        hintText.fontSize = 12;
        hintText.alignment = TextAnchor.LowerRight;
        hintText.color = Color.yellow;
        hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        hintText.verticalOverflow = VerticalWrapMode.Truncate;
        var hintLayout = hintText.GetComponent<LayoutElement>() ?? hintText.gameObject.AddComponent<LayoutElement>();
        hintLayout.preferredHeight = 20f;
        hintLayout.flexibleHeight = 0f;
    }

    void EnsureHighlightMarker()
    {
        if (canvas == null) return;
        if (highlightMarker != null) return;

        GameObject markerGO = new GameObject("HighlightMarker");
        markerGO.transform.SetParent(canvas.transform, false);
        highlightMarker = markerGO.AddComponent<RectTransform>();

        var img = markerGO.AddComponent<Image>();
        img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        img.color = highlightColor;
        highlightMarker.sizeDelta = highlightSize;
        highlightMarker.anchorMin = new Vector2(0.5f, 0.5f);
        highlightMarker.anchorMax = new Vector2(0.5f, 0.5f);
        highlightMarker.pivot = new Vector2(0.5f, 0.5f);
        highlightMarker.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isVisible) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            OnNextPressed?.Invoke();

        if (currentStep != null && currentStep.highlightTarget != null && highlightMarker != null && highlightMarker.gameObject.activeSelf)
            UpdateHighlightMarker();
    }

    void UpdateHighlightMarker()
    {
        if (currentStep == null || currentStep.highlightTarget == null || canvas == null) return;

        Camera cam = Camera.main;
        if (cam == null) { highlightMarker.gameObject.SetActive(false); return; }

        Vector3 worldPos = currentStep.highlightTarget.position;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f) { highlightMarker.gameObject.SetActive(false); return; }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPoint);

        highlightMarker.anchoredPosition = localPoint;

        float distance = Vector3.Distance(cam.transform.position, worldPos);
        float scale = Mathf.Clamp(1f / Mathf.Max(0.1f, distance) * 5f, 0.6f, 1.6f);
        highlightMarker.sizeDelta = highlightSize * scale;

        highlightMarker.gameObject.SetActive(true);
    }

    void AdjustPanelSizeToContent()
    {
        if (panel == null) return;

        var vlayout = panel.GetComponent<VerticalLayoutGroup>();
        RectOffset padding = vlayout != null ? vlayout.padding : new RectOffset(10, 10, 10, 10);

        int childCount = panel.childCount;
        float maxPreferredWidth = 0f;
        for (int i = 0; i < childCount; i++)
        {
            var child = panel.GetChild(i) as RectTransform;
            if (child == null) continue;
            float pw = LayoutUtility.GetPreferredWidth(child);
            if (pw > maxPreferredWidth) maxPreferredWidth = pw;
        }

        float canvasScale = 1f;
        if (canvas != null) canvasScale = Mathf.Max(0.0001f, canvas.scaleFactor);

        float sidePaddingPx = 40f;
        float availableWidthPx = Mathf.Max(120f, Screen.width - sidePaddingPx * 2f);
        float availableWidthCanvas = availableWidthPx / canvasScale;

        float targetWidth = Mathf.Clamp(maxPreferredWidth + padding.left + padding.right, minPanelWidth, maxPanelWidth);
        targetWidth = Mathf.Min(targetWidth, availableWidthCanvas);

        panel.sizeDelta = new Vector2(targetWidth, panel.sizeDelta.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        float spacing = vlayout != null ? vlayout.spacing : 6f;
        float totalHeight = padding.top + padding.bottom;
        float maxPreferredWidthAfter = 0f;
        for (int i = 0; i < childCount; i++)
        {
            var child = panel.GetChild(i) as RectTransform;
            if (child == null) continue;

            float ph = LayoutUtility.GetPreferredHeight(child);
            float pw = LayoutUtility.GetPreferredWidth(child);
            totalHeight += ph;
            if (i < childCount - 1) totalHeight += spacing;
            if (pw > maxPreferredWidthAfter) maxPreferredWidthAfter = pw;
        }

        totalHeight += 4f;

        float maxAllowedHeightPx = Mathf.Max(60f, Screen.height * 0.5f - topPadding * 2f);
        float maxAllowedHeightCanvas = maxAllowedHeightPx / canvasScale;
        float targetHeight = Mathf.Clamp(totalHeight, minPanelHeight, Mathf.Min(maxPanelHeight, maxAllowedHeightCanvas));

        panel.sizeDelta = new Vector2(targetWidth, targetHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        float topPaddingCanvas = topPadding / canvasScale;
        panel.anchoredPosition = new Vector2(0f, -topPaddingCanvas);
    }

    IEnumerator SafeRebuildLayout()
    {
        layoutCoroutine = null;

        yield return null;

        if (panel == null) yield break;

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        AdjustPanelSizeToContent();

        Canvas.ForceUpdateCanvases();

        layoutCoroutine = null;
    }

    public void ShowStep(TutorialStep step)
    {
        currentStep = step;

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(step.stepName) ? step.stepType.ToString() : step.stepName;

        if (descriptionText != null)
            descriptionText.text = step.description ?? string.Empty;

        if (hintText != null)
            hintText.text = step.requireEnterToProceed ? "Press Enter to continue" : (step.autoDuration > 0f ? $"Auto-advances in {step.autoDuration:F0}s" : "Press Enter to continue");

        if (panel != null)
        {
            EnsurePanelSetup();

            panel.gameObject.SetActive(true);

            if (layoutCoroutine != null)
            {
                StopCoroutine(layoutCoroutine);
                layoutCoroutine = null;
            }
            layoutCoroutine = StartCoroutine(SafeRebuildLayout());
        }

        EnsureHighlightMarker();
        if (highlightMarker != null)
            highlightMarker.gameObject.SetActive(step.highlightTarget != null);

        Debug.Log($"[TutorialUI] ShowStep: '{step.stepName}' description length={ (step.description?.Length ?? 0) }");

        isVisible = true;
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
        if (highlightMarker != null) highlightMarker.gameObject.SetActive(false);
        isVisible = false;
        currentStep = null;
    }

    public bool IsVisible() => isVisible;
}
