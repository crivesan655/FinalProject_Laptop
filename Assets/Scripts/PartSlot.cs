using UnityEngine;

public class PartSlot : MonoBehaviour
{
    [Header("Slot Settings")]
    public PartType allowedType;
    public bool isOccupied;

    [Header("Editor Positioning")]
    [Tooltip("Optional target object whose transform will be used to position this PartSlot.")]
    public Transform targetObject;
    [Tooltip("If true, snap this PartSlot to the target when the target is assigned / changed in the Inspector.")]
    public bool snapOnAssign = true;
    [Tooltip("If true, match the target's rotation when snapping.")]
    public bool matchRotation = true;
    [Tooltip("If true (Editor only), continuously follow the target while editing.")]
    public bool followTargetInEditor = false;

    public void PlacePart(PartInfo part)
    {
        isOccupied = true;
        part.isInstalled = true;
    }

    public void RemovePart(PartInfo part)
    {
        isOccupied = false;
        part.isInstalled = false;
    }

    // Called when a value changes in the Inspector (Editor and Prefab edits).
    void OnValidate()
    {
        // Only snap automatically when a target is defined and snapOnAssign enabled.
        if (targetObject != null && snapOnAssign)
        {
            SnapToTarget();
        }
    }

    // Allow manual snapping from the Inspector context menu.
    [ContextMenu("Snap To Target")]
    public void SnapToTarget()
    {
        if (targetObject == null) return;

        // Move this GameObject to the center of the target object's transform.
        transform.position = targetObject.position;

        if (matchRotation)
            transform.rotation = targetObject.rotation;
    }

#if UNITY_EDITOR
    void Update()
    {
        // In edit mode, optionally follow the target as it moves.
        if (!Application.isPlaying && followTargetInEditor && targetObject != null)
        {
            transform.position = targetObject.position;
            if (matchRotation)
                transform.rotation = targetObject.rotation;
        }
    }
#endif

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.05f);

        // If a target is assigned, draw a faint line to it for clarity in the editor.
        if (targetObject != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawLine(transform.position, targetObject.position);
        }
    }
}