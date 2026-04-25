using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StepType
{
    Dismantle,
    Clean,
    Repair,
    Assemble,
    Info
}

[System.Serializable]
public class TutorialStep
{
    public string stepName = "Step";
    [TextArea(3, 8)]
    public string description = "Describe what to do in this step.";

    public StepType stepType = StepType.Info;

    public Transform highlightTarget;

    public bool requireEnterToProceed = true;

    public float autoDuration = 0f;
}
