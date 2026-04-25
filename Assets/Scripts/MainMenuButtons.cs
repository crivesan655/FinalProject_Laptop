using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuButtons : MonoBehaviour
{
    public float maintenanceDisplaySeconds = 2.0f;
    bool maintenanceShowing = false;

    public void LoadTutorial()
    {
        SceneManager.LoadScene("TutorialMap");
    }

    public void LoadGame()
    {
        if (!maintenanceShowing)
            StartCoroutine(ShowMaintenanceMessage());
    }

    IEnumerator ShowMaintenanceMessage()
    {
        maintenanceShowing = true;

        // Create Canvas
        GameObject canvasGO = new GameObject("MaintenanceCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Panel (background)
        GameObject panelGO = new GameObject("MaintenancePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(420f, 140f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);

        // Create Text
        GameObject textGO = new GameObject("MaintenanceText");
        textGO.transform.SetParent(panelGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(12f, 12f);
        textRect.offsetMax = new Vector2(-12f, -12f);

        var txt = textGO.AddComponent<Text>();
        // Use LegacyRuntime.ttf (supported built-in runtime font)
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = "In maintenance";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 28;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        // Optional simple fade in
        var canvasGroup = panelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        float fadeTime = 0.15f;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Wait with unscaled time so UI displays even if timeScale changes
        float timer = 0f;
        while (timer < maintenanceDisplaySeconds)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeTime);
            yield return null;
        }

        Destroy(canvasGO);
        maintenanceShowing = false;
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exit Button Pressed... Exiting Program");
    }
}
