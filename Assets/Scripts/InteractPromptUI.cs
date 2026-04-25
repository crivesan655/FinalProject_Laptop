using UnityEngine;
using TMPro;

public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance;

    [Header("References")]
    public CanvasGroup canvasGroup;
    public TMP_Text promptText;
    public Camera followCamera;

    [Header("World Space Follow")]
    public Vector3 worldOffset = new Vector3(0f, 0.25f, 0f);
    public float followSpeed = 12f;
    public bool faceCamera = true;

    [Header("Fade")]
    public float fadeSpeed = 8f;

    float targetAlpha = 0f;
    Transform target;

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

        if (followCamera == null)
            followCamera = Camera.main;
    }

    void Start()
    {
        if (promptText != null && string.IsNullOrEmpty(promptText.text))
            promptText.text = "E";

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    void Update()
    {
        HandleFade();

        if (target != null)
            UpdatePosition();
    }

    void HandleFade()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    void UpdatePosition()
    {
        if (target == null) return;

        Vector3 desired = ComputeBestPosition(target);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);

        if (faceCamera && followCamera != null)
        {
            Vector3 dir = transform.position - followCamera.transform.position;
            if (dir.sqrMagnitude > Mathf.Epsilon)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    // Compute best world position for the prompt so it's not occluded by the target model.
    Vector3 ComputeBestPosition(Transform targetTransform)
    {
        // gather renderers
        var renderers = targetTransform.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return targetTransform.position + worldOffset;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 center = bounds.center;
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        float margin = 0.25f + maxExtent * 0.1f;

        Camera cam = followCamera ?? Camera.main;
        Vector3 camPos = cam.transform.position;

        // candidate directions: away from camera, up, right/left relative to target, forward/back
        Vector3 away = (center - camPos).normalized;
        Vector3[] dirs = new Vector3[]
        {
            away,
            Vector3.up,
            Vector3.up * 0.5f + away * 0.5f,
            targetTransform.right,
            -targetTransform.right,
            targetTransform.forward,
            -targetTransform.forward
        };

        foreach (var d in dirs)
        {
            Vector3 candidate = center + d.normalized * (maxExtent + margin);

            // raycast from camera to candidate, check if anything blocks
            Vector3 rayDir = candidate - camPos;
            float rayDist = rayDir.magnitude;
            if (rayDist <= Mathf.Epsilon)
                return candidate; // degenerate

            if (Physics.Raycast(camPos, rayDir.normalized, out RaycastHit hit, rayDist))
            {
                // if the ray hits the target's own renderers (i.e. hit.collider is child of target), candidate is behind model => try another
                if (hit.collider != null && hit.collider.transform.IsChildOf(targetTransform))
                {
                    continue;
                }
                // hit something else (scene object) blocking candidate -> try next
                continue;
            }
            // no hit -> clear view
            return candidate;
        }

        // fallback: place above center
        return center + Vector3.up * (maxExtent + margin);
    }

    // Show and anchor the prompt to a world transform (attach point / part)
    public void ShowAt(Transform targetTransform, Vector3 offset)
    {
        if (promptText != null && string.IsNullOrEmpty(promptText.text))
            promptText.text = "E";

        target = targetTransform;
        worldOffset = offset;
        targetAlpha = 1f;

        // initial position
        transform.position = ComputeBestPosition(targetTransform);
    }

    public void Show(string text = "E")
    {
        if (promptText != null)
            promptText.text = text;

        targetAlpha = 1f;
    }

    public void Hide()
    {
        target = null;
        targetAlpha = 0f;
    }
}
