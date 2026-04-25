using UnityEngine;

public class AttachablePart : MonoBehaviour
{
    public Transform attachPoint;
    public float snapDistance = 1f;
    public bool isTool = false;

    private bool attached;
    private Rigidbody rb;

    private PartSlot targetSlot;
    private PartSlot currentSlot;

    private PartInfo partInfo;
    private Collider[] colliders;

    // Expose attachment state
    public bool IsAttached => attached;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        partInfo = GetComponent<PartInfo>();
        colliders = GetComponentsInChildren<Collider>();

        if (partInfo == null)
        {
            Debug.LogError(gameObject.name + " is missing PartInfo!");
            return;
        }

        if (partInfo.currentState == PartState.Assembled)
        {
            attached = true;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                // kinematic + speculative continuous to better detect collisions with moving objects
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            foreach (var col in colliders)
                col.enabled = true; // keep colliders ON

            try
            {
                gameObject.tag = "Draggable";
            }
            catch { }
        }
    }

    void Update()
    {
        if (attached || isTool) return;
        FindClosestSlot();
    }

    void FindClosestSlot()
    {
        if (partInfo == null) return;

        PartSlot[] slots = FindObjectsOfType<PartSlot>();
        float closestDistance = Mathf.Infinity;
        PartSlot closest = null;

        foreach (PartSlot slot in slots)
        {
            if (slot == null) continue;
            if (slot.isOccupied) continue;
            if (partInfo.partType != slot.allowedType) continue;

            float dist = Vector3.Distance(transform.position, slot.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = slot;
            }
        }

        targetSlot = (closestDistance <= snapDistance) ? closest : null;
    }

    public bool CheckSnap()
    {
        if (isTool) return false;

        if (attached) return false;
        if (targetSlot == null) return false;
        if (targetSlot.isOccupied) return false;
        if (partInfo.partType != targetSlot.allowedType) return false;

        float dist = Vector3.Distance(transform.position, targetSlot.transform.position);

        if (dist <= snapDistance)
        {
            Snap(targetSlot);
            return true;
        }

        return false;
    }

    void Snap(PartSlot slot)
    {
        attached = true;
        currentSlot = slot;

        Vector3 worldScale = transform.lossyScale;

        transform.position = attachPoint.position;
        transform.rotation = attachPoint.rotation;
        transform.SetParent(attachPoint);

        Vector3 parentScale = attachPoint.lossyScale;

        transform.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            worldScale.z / parentScale.z
        );

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        foreach (var col in colliders)
            col.enabled = true;

        try
        {
            gameObject.tag = "Draggable";
        }
        catch { }

        if (slot != null)
            slot.PlacePart(partInfo);

        partInfo.currentState = PartState.Assembled;
    }

    public void Detach()
    {
        attached = false;

        transform.parent = null;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        foreach (var col in colliders)
            col.enabled = true;

        try
        {
            gameObject.tag = "Draggable";
        }
        catch { }

        if (currentSlot != null)
        {
            currentSlot.RemovePart(partInfo);
            currentSlot = null;
        }

        targetSlot = null;
    }

    public bool TryDetach()
    {
        // Prevent detaching screen type entirely (screen toggles open/close instead)
        if (partInfo != null && partInfo.partType == PartType.Screen)
        {
            Debug.Log($"[AttachablePart] '{gameObject.name}' is a Screen and cannot be detached.");
            ShowInvalidFeedback();
            return false;
        }

        ToolManager tm = FindObjectOfType<ToolManager>();

        if (partInfo == null)
        {
            Debug.LogWarning($"[AttachablePart] '{gameObject.name}' TryDetach failed: missing PartInfo.");
            return false;
        }

        if (!string.IsNullOrEmpty(partInfo.requiredTool))
        {
            if (tm == null || !tm.HasTool(partInfo.requiredTool))
            {
                Debug.Log($"[AttachablePart] '{gameObject.name}' TryDetach blocked: required tool '{partInfo.requiredTool}' not present.");
                ShowInvalidFeedback();
                return false;
            }
        }

        if (partInfo.currentState != PartState.Assembled)
        {
            Debug.Log($"[AttachablePart] '{gameObject.name}' TryDetach blocked: currentState is {partInfo.currentState} (needs Assembled).");
            return false;
        }

        if (!partInfo.CanBeRemoved() || partInfo.HasNearbyBlockingParts(partInfo.dependencyProximity))
        {
            if (partInfo.HasNearbyBlockingParts(partInfo.dependencyProximity))
                Debug.Log($"[AttachablePart] '{gameObject.name}' TryDetach blocked: a required part is still close (within {partInfo.dependencyProximity} units).");

            if (!partInfo.CanBeRemoved())
            {
                if (partInfo.requiredRemovedParts != null)
                {
                    string deps = "";
                    foreach (var p in partInfo.requiredRemovedParts)
                        if (p != null)
                            deps += $"{p.partName}({p.currentState}), ";
                    Debug.Log($"[AttachablePart] '{gameObject.name}' TryDetach blocked: dependencies not removed -> {deps}");
                }
                else
                {
                    Debug.Log($"[AttachablePart] '{gameObject.name}' TryDetach blocked: CanBeRemoved returned false.");
                }
            }

            ShowInvalidFeedback();
            return false;
        }

        partInfo.currentState = PartState.Disassembled;

        Detach();
        Debug.Log($"[AttachablePart] '{gameObject.name}' detached successfully.");
        return true;
    }

    void ShowInvalidFeedback()
    {
        Outline outline = GetComponentInChildren<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = Color.red;

            CancelInvoke(nameof(ResetOutline));
            Invoke(nameof(ResetOutline), 0.5f);
        }
    }

    void ResetOutline()
    {
        Outline outline = GetComponentInChildren<Outline>();
        if (outline != null)
            outline.enabled = false;
    }
}