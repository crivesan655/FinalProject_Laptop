using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TutorialManager : MonoBehaviour
{
    [Header("Steps (configure in inspector)")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Behavior")]
    public bool startOnAwake = false;
    public bool loopWhenFinished = false;

    int currentIndex = -1;
    Coroutine autoAdvanceCoroutine;
    TutorialUI ui;

    void Awake()
    {
        ui = FindObjectOfType<TutorialUI>();
        if (ui == null)
        {

            var go = new GameObject("TutorialUI");
            ui = go.AddComponent<TutorialUI>();
        }

        ui.OnNextPressed += OnUIRequestNext;
    }

    void Start()
    {
        if (startOnAwake)
            StartTutorial();
    }

    public void StartTutorial()
    {
        if (steps == null || steps.Count == 0) return;
        currentIndex = -1;
        NextStep();
    }

    public void NextStep()
    {
        if (autoAdvanceCoroutine != null) { StopCoroutine(autoAdvanceCoroutine); autoAdvanceCoroutine = null; }

        currentIndex++;
        if (currentIndex >= steps.Count)
        {
            if (loopWhenFinished) currentIndex = 0;
            else { EndTutorial(); return; }
        }

        ShowCurrentStep();
    }

    public void PrevStep()
    {
        if (autoAdvanceCoroutine != null) { StopCoroutine(autoAdvanceCoroutine); autoAdvanceCoroutine = null; }

        currentIndex = Mathf.Max(0, currentIndex - 1);
        ShowCurrentStep();
    }

    void ShowCurrentStep()
    {
        if (currentIndex < 0 || currentIndex >= steps.Count)
        {
            EndTutorial();
            return;
        }

        var step = steps[currentIndex];

        ui.ShowStep(step);


        if (!step.requireEnterToProceed && step.autoDuration > 0f)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance(step.autoDuration));
        }

        if (step.highlightTarget != null)
        {
            Debug.Log($"[TutorialManager] Highlight target for step '{step.stepName}': {step.highlightTarget.name}");
        }
    }

    IEnumerator AutoAdvance(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }
        NextStep();
    }

    void EndTutorial()
    {
        ui.Hide();
        currentIndex = -1;
    }

    void OnUIRequestNext()
    {
        if (currentIndex < 0 || currentIndex >= steps.Count) return;
        var step = steps[currentIndex];
        if (step.requireEnterToProceed)
        {
            NextStep();
        }
    }

    [ContextMenu("Advance Step")]
    void EditorAdvance() => NextStep();
}
