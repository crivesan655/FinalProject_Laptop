using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RotationModeUI : MonoBehaviour
{
    public static RotationModeUI Instance;

    public CanvasGroup canvasGroup;
    public TMP_Text messageText;
    public float fadeSpeed = 8f;

    [Header("Default")]
    [Tooltip("Default message to show when entering rotation mode. If empty, the current text on the TMP_Text will be used.")]
    public string defaultMessage = "Rotation Mode Enabled - Press R or Let go of the left click to Exit";

    float targetAlpha = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>();

        if (messageText != null && !string.IsNullOrEmpty(defaultMessage))
            messageText.text = defaultMessage;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    public void Show(string message = null)
    {
        if (messageText != null && !string.IsNullOrEmpty(message))
            messageText.text = message;

        targetAlpha = 1f;
    }

    public void Hide()
    {
        targetAlpha = 0f;
    }
}
