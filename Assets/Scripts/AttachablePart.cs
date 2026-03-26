using UnityEngine;

public class AttachablePart : MonoBehaviour
{
    public Transform attachPoint;
    public float snapDistance = 1.0f; 

    private bool isAttached = false; 
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    public bool CheckSnap()
    {
        if (attachPoint == null) return false;

        float dist = Vector3.Distance(transform.position, attachPoint.position);

        if (dist <= snapDistance)
        {
            SnapToPoint();
            return true;
        }

        return false;
    }
    private void SnapToPoint()
    {
        isAttached = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            // rb.angularVelocity = Vector3.zero;
        }

        transform.position = attachPoint.position;
        transform.rotation = attachPoint.rotation;
        transform.parent = attachPoint;

        transform.localScale = Vector3.one;
    }
    public void Detach()
    {
        isAttached = false;
        if (rb != null) rb.isKinematic = false;
        transform.parent = null;
    }

    public void TryDetach()
    {
        if (!isAttached) return;
        Detach();
    }
}