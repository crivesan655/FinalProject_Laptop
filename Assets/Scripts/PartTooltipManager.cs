using UnityEngine;
using TMPro;

public class PartTooltipManager : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject tooltipPrefab;

    public float showDelay = 0.3f;
    public float fadeSpeed = 8f;
    public float moveSpeed = 10f;

    [Header("Tooltip Appearance")]
    [Tooltip("Base font size used by the tooltip (title/description scale is kept via rich text % tags).")]
    public float tooltipBaseFontSize = 14f;
    [Tooltip("Fixed world scale applied to the tooltip GameObject. Keeps text size consistent regardless of camera distance.")]
    public float tooltipScale = 0.6f;

    [Header("Filtering")]
    [Tooltip("Layers to ignore for tooltip raycasts/overlap checks (e.g. the Player layer). Assign your Player layer here.")]
    public LayerMask ignoreLayerMask = 0;

    private GameObject currentTooltip;
    private TMP_Text tooltipText;
    private CanvasGroup canvasGroup;

    private PartInfo hoveredPart;

    private float hoverTimer;
    private bool isVisible;

    // tuning
    const float tooltipForwardDistance = 1.8f;
    const float candidateOffset = 0.9f;
    const float checkSphereRadius = 0.18f; // small radius to test for nearby geometry

    // avoids placing tooltip right in the center of the screen
    const float centerAvoidRadius = 0.25f; // viewport units (0..0.5)
    const float minDistanceFromCamera = 1.2f;

    void Start()
    {
        currentTooltip = Instantiate(tooltipPrefab);
        tooltipText = currentTooltip.GetComponentInChildren<TMP_Text>(true);
        canvasGroup = currentTooltip.GetComponent<CanvasGroup>();

        // Apply inspector-configured base font size and disable auto-size to keep size stable.
        if (tooltipText != null)
        {
            tooltipText.enableAutoSizing = false;
            tooltipText.fontSize = tooltipBaseFontSize;
        }

        canvasGroup.alpha = 0f;
        currentTooltip.SetActive(true);

        // Ensure a consistent initial scale
        if (currentTooltip != null)
            currentTooltip.transform.localScale = Vector3.one * tooltipScale;
    }

    void Update()
    {
        if (playerCamera == null) return;

        // build an inclusive mask that excludes the ignoreLayerMask bits
        int includeMask = ~ignoreLayerMask.value;

        // Cast all hits and pick the first valid PartInfo, ignoring the player itself.
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5f, includeMask);

        PartInfo part = null;

        if (hits != null && hits.Length > 0)
        {
            // sort by distance so we consider closest first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider == null) continue;

                // ignore any collider that belongs to the player (camera root/player controller or tagged Player)
                if (IsPlayerCollider(h.collider)) continue;

                part = h.collider.GetComponentInParent<PartInfo>();
                if (part != null)
                {
                    // found the part we want
                    break;
                }
            }
        }

        bool hitValidPart = false;

        if (part != null)
        {
            hitValidPart = true;

            if (hoveredPart != part)
            {
                hoveredPart = part;
                hoverTimer = 0f;
                isVisible = false;
            }

            hoverTimer += Time.deltaTime;

            if (hoverTimer >= showDelay)
            {
                isVisible = true;

                tooltipText.text =
                    "<size=120%><b>" + part.partName + "</b></size>\n\n" +
                    "<size=80%>" + part.description + "</size>";

                UpdateTooltipPosition(part, includeMask);
            }
        }

        if (!hitValidPart)
        {
            hoveredPart = null;
            hoverTimer = 0f;
            isVisible = false;
        }

        HandleFade();
    }

    void HandleFade()
    {
        float targetAlpha = isVisible ? 1f : 0f;

        canvasGroup.alpha = Mathf.Lerp(
            canvasGroup.alpha,
            targetAlpha,
            Time.deltaTime * fadeSpeed
        );
    }

    // Return true when collider belongs to the player's hierarchy, is tagged "Player", or has a PlayerController
    bool IsPlayerCollider(Collider c)
    {
        if (c == null || playerCamera == null) return false;

        // If collider is explicitly tagged "Player" (checks GameObject and parents)
        Transform t = c.transform;
        while (t != null)
        {
            if (t.CompareTag("Player"))
                return true;
            t = t.parent;
        }

        // If collider is part of the camera's root (common setup: camera is child of player root)
        var cameraRoot = playerCamera.transform.root;
        if (c.transform.IsChildOf(cameraRoot)) return true;

        // Also treat anything that has a PlayerController in parents as player
        if (c.GetComponentInParent<PlayerController>() != null) return true;

        return false;
    }

    void UpdateTooltipPosition(PartInfo part, int includeMask)
    {
        if (part == null || playerCamera == null || currentTooltip == null) return;

        // Direction from part to camera
        Vector3 toCamera = (playerCamera.transform.position - part.transform.position);
        float distanceToCamera = toCamera.magnitude;
        Vector3 dirToCamera = toCamera.normalized;

        // Base candidate position: slightly towards the camera from the part
        Vector3 basePosition = part.transform.position + dirToCamera * tooltipForwardDistance;

        // Use camera's right/up so tooltip faces camera consistently
        Vector3 right = playerCamera.transform.right;
        Vector3 up = playerCamera.transform.up;

        // Candidate offsets around base (include center + 5 offsets)
        Vector3[] candidates = new Vector3[]
        {
            basePosition,                                            // center
            basePosition + right * candidateOffset,                 // right
            basePosition - right * candidateOffset,                 // left
            basePosition + up * candidateOffset,                    // up
            basePosition - up * candidateOffset,                    // down
            basePosition + (right + up).normalized * candidateOffset // diag
        };

        float bestScore = -Mathf.Infinity;
        Vector3 bestPos = basePosition;

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 cand = candidates[i];

            // Skip candidates that are behind the camera
            Vector3 camToCand = cand - playerCamera.transform.position;
            if (camToCand.sqrMagnitude < 1e-6f) continue;
            if (Vector3.Dot(playerCamera.transform.forward, camToCand.normalized) < -0.05f)
                continue;

            float candDist = camToCand.magnitude;
            Vector3 camDirToCand = camToCand.normalized;
            float visibilityScore = Vector3.Dot(playerCamera.transform.forward, camDirToCand); // -1..1

            // Line-of-sight: check if anything blocks camera -> candidate (ignore the part itself and player)
            bool blocked = false;
            if (Physics.Raycast(playerCamera.transform.position, camDirToCand, out RaycastHit hitInfo, candDist, includeMask))
            {
                if (hitInfo.collider != null)
                {
                    if (!hitInfo.collider.transform.IsChildOf(part.transform) && !IsPlayerCollider(hitInfo.collider))
                        blocked = true;
                }
            }
            float obstructionPenalty = blocked ? -3.0f : 0f;

            // Proximity to world geometry: ensure candidate isn't overlapping a wall/floor (ignore player colliders)
            bool overlap = false;
            Collider[] overlaps = Physics.OverlapSphere(cand, checkSphereRadius, includeMask, QueryTriggerInteraction.Ignore);
            if (overlaps != null && overlaps.Length > 0)
            {
                foreach (var o in overlaps)
                {
                    if (o == null) continue;
                    if (o.transform.IsChildOf(part.transform)) continue;
                    if (IsPlayerCollider(o)) continue;
                    overlap = true;
                    break;
                }
            }
            float overlapPenalty = overlap ? -2.5f : 0f;

            // Ground check: penalize positions that are too close to ground/wall (ignore player)
            float groundPenalty = 0f;
            if (Physics.Raycast(cand, Vector3.down, out RaycastHit groundHit, 0.6f, includeMask))
            {
                if (!groundHit.collider.transform.IsChildOf(part.transform) && !IsPlayerCollider(groundHit.collider))
                    groundPenalty = -1.0f;
            }

            // Avoid center of camera view: heavy penalty if candidate projects near screen center
            Vector3 vp = playerCamera.WorldToViewportPoint(cand);
            float centerPenalty = 0f;
            if (vp.z > 0f)
            {
                Vector2 vp2 = new Vector2(vp.x, vp.y);
                float centerDist = Vector2.Distance(vp2, new Vector2(0.5f, 0.5f));
                if (centerDist < centerAvoidRadius)
                    centerPenalty = -4.0f;
            }
            else
            {
                // behind camera, heavy penalty
                centerPenalty = -5.0f;
            }

            // Enforce minimum distance from camera so tooltip doesn't sit right in front of player view
            float distancePenalty = 0f;
            if (candDist < minDistanceFromCamera)
                distancePenalty = -3.0f;

            // Compose score (visibility weighted, penalize obstruction/overlap/center)
            float score = visibilityScore * 1.0f + obstructionPenalty + overlapPenalty + groundPenalty + centerPenalty + distancePenalty;

            // Small distance bias prefer closer to camera but not too close
            float preferredDist = Mathf.Clamp(distanceToCamera * 0.6f, 0.6f, 2.5f);
            float distBias = -Mathf.Abs(candDist - preferredDist) * 0.2f;
            score += distBias;

            if (score > bestScore)
            {
                bestScore = score;
                bestPos = cand;
            }
        }

        // If the best candidate is still poor, place tooltip to the player's right-side (non-obtrusive fallback)
        if (bestScore < -4.5f)
        {
            bestPos = playerCamera.transform.position
                    + playerCamera.transform.right * 1.2f
                    + playerCamera.transform.forward * 1.5f
                    - playerCamera.transform.up * 0.2f;
        }

        // ensure tooltip is at least slightly above part
        float minY = part.transform.position.y + 0.5f;
        if (bestPos.y < minY) bestPos.y = minY;

        // Smooth move
        currentTooltip.transform.position = Vector3.Lerp(
            currentTooltip.transform.position,
            bestPos,
            Time.deltaTime * moveSpeed
        );

        // Face camera
        currentTooltip.transform.LookAt(playerCamera.transform);
        currentTooltip.transform.Rotate(0f, 180f, 0f);

        // Use a fixed scale so font size remains consistent regardless of camera distance
        currentTooltip.transform.localScale = Vector3.one * tooltipScale;
    }
}