using UnityEngine;

public class PartInteractionController : MonoBehaviour
{
    void Update()
    {
        GameObject obj = InteractionRaycaster.currentLookObject;

#if UNITY_EDITOR
        if (obj == null)
            Debug.Log("[PartInteractionController] currentLookObject == null");
        else
            Debug.Log($"[PartInteractionController] currentLookObject = {obj.name}");
#endif

        // Show or hide the on-screen "E" prompt based on the object under the crosshair
        if (obj != null)
        {
            // Try to get PartInfo and AttachablePart either on the object or its parents.
            PartInfo info = obj.GetComponentInParent<PartInfo>();
            AttachablePart part = obj.GetComponentInParent<AttachablePart>();

            // --- NEW FALLBACK: if object is the Laptop root (no PartInfo found on it)
            // try to inspect the exact collider that the raycast hit (InteractionRaycaster.lastHit).
            if ((info == null || part == null) && obj.CompareTag("Laptop"))
            {
                var hit = InteractionRaycaster.lastHit;
                if (hit.collider != null)
                {
                    // prefer components on the exact collider first, then its parents
                    var hitPart = hit.collider.GetComponent<AttachablePart>() ?? hit.collider.GetComponentInParent<AttachablePart>();
                    var hitInfo = hit.collider.GetComponent<PartInfo>() ?? hit.collider.GetComponentInParent<PartInfo>();

#if UNITY_EDITOR
                    if (hit.collider != null)
                        Debug.Log($"[PartInteractionController] Laptop fallback - hit.collider = '{hit.collider.gameObject.name}', found AttachablePart = {(hitPart!=null ? hitPart.gameObject.name : "null")}, PartInfo = {(hitInfo!=null ? hitInfo.gameObject.name : "null")}");
#endif

                    // use these when available
                    if (hitPart != null) part = hitPart;
                    if (hitInfo != null) info = hitInfo;
                }
            }

            if (part != null && info != null && info.currentState == PartState.Assembled)
            {
                // Only show if the part can be removed and no blocking deps nearby
                if (info.CanBeRemoved() && !info.HasNearbyBlockingParts(info.dependencyProximity))
                {
                    Transform anchor = part.attachPoint != null ? part.attachPoint : part.transform;
                    Vector3 offset = Vector3.up * 0.25f;
                    InteractPromptUI.Instance?.ShowAt(anchor, offset);
                }
                else
                {
                    InteractPromptUI.Instance?.Hide();
                }
            }
            else
            {
                InteractPromptUI.Instance?.Hide();
            }
        }
        else
        {
            InteractPromptUI.Instance?.Hide();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (obj == null) return;

            // robustly find the AttachablePart (in parent or self) - include fallback via lastHit for laptop
            AttachablePart part = obj.GetComponentInParent<AttachablePart>();
            PartInfo info = obj.GetComponentInParent<PartInfo>();

            if ((part == null || info == null) && obj.CompareTag("Laptop"))
            {
                var hit = InteractionRaycaster.lastHit;
                if (hit.collider != null)
                {
                    part = part ?? (hit.collider.GetComponent<AttachablePart>() ?? hit.collider.GetComponentInParent<AttachablePart>());
                    info = info ?? (hit.collider.GetComponent<PartInfo>() ?? hit.collider.GetComponentInParent<PartInfo>());
                }
            }

            if (part != null)
            {
#if UNITY_EDITOR
                Debug.Log($"[PartInteractionController] Attempting TryDetach on '{part.gameObject.name}'");
#endif
                bool result = part.TryDetach();
#if UNITY_EDITOR
                Debug.Log($"[PartInteractionController] TryDetach returned {result} for '{part.gameObject.name}'");
#endif
                if (result)
                {
                    InteractPromptUI.Instance?.Hide();
                }
            }
            else if (info != null)
            {
#if UNITY_EDITOR
                Debug.Log($"[PartInteractionController] Found PartInfo but no AttachablePart on '{info.gameObject.name}'");
#endif
                
            }
        }
    }
}