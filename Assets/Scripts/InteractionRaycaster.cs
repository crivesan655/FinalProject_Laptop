using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    public Camera cam;
    public float range = 5f;

    // Public debugable last hit so other systems (PartInteractionController) can inspect the exact collider hit.
    public static GameObject currentLookObject;
    public static RaycastHit lastHit; // new: store the last RaycastHit when a hit occurs

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            // store hit for other systems to inspect
            lastHit = hit;

            AttachablePart part = hit.collider.GetComponentInParent<AttachablePart>();
            ToolItem tool = hit.collider.GetComponentInParent<ToolItem>();
            PartInfo pinfo = hit.collider.GetComponentInParent<PartInfo>();

#if UNITY_EDITOR
            Debug.DrawLine(ray.origin, hit.point, Color.yellow, 0.05f);
#endif

            if (part != null)
            {
#if UNITY_EDITOR
                Debug.Log($"[InteractionRaycaster] hit '{hit.collider.gameObject.name}' -> AttachablePart '{part.gameObject.name}' (tag='{part.gameObject.tag}')");
#endif
                currentLookObject = part.gameObject;
            }
            else if (tool != null)
            {
#if UNITY_EDITOR
                Debug.Log($"[InteractionRaycaster] hit '{hit.collider.gameObject.name}' -> Tool '{tool.gameObject.name}' (tag='{tool.gameObject.tag}')");
#endif
                currentLookObject = tool.gameObject;
            }
            else if (pinfo != null)
            {
#if UNITY_EDITOR
                Debug.Log($"[InteractionRaycaster] hit '{hit.collider.gameObject.name}' -> PartInfo '{pinfo.gameObject.name}' (state={pinfo.currentState})");
#endif
                currentLookObject = pinfo.gameObject;
            }
            else
            {
                // If ray hit something that is part of the laptop root, allow selecting the laptop root by tag.
                Transform t = hit.collider.transform;
                GameObject laptop = null;
                while (t != null)
                {
                    if (t.CompareTag("Laptop"))
                    {
                        laptop = t.gameObject;
                        break;
                    }
                    t = t.parent;
                }

                if (laptop != null)
                {
#if UNITY_EDITOR
                    Debug.Log($"[InteractionRaycaster] hit '{hit.collider.gameObject.name}' -> Laptop '{laptop.name}'");
#endif
                    currentLookObject = laptop;
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log($"[InteractionRaycaster] hit '{hit.collider.gameObject.name}' -> nothing relevant (tag='{hit.collider.gameObject.tag}')");
#endif
                    currentLookObject = null;
                }
            }
        }
        else
        {
            currentLookObject = null;
            lastHit = default;
        }
    }
}