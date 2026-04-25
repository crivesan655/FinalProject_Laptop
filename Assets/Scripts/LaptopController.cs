using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LaptopController : MonoBehaviour
{
    [Header("Screen")]
    public Transform screenTransform;          
    public Vector3 closedEuler = new Vector3(0f, 0f, 0f);
    public Vector3 openEuler = new Vector3(-110f, 0f, 0f);
    public float openCloseSpeed = 6f;

    bool isOpen = false;
    Coroutine anim;

    void Reset()
    {
        var t = transform.Find("Screen");
        if (t != null) screenTransform = t;
    }

    public void ToggleScreen()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (screenTransform == null) return;

        isOpen = open;

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateToRotation(open ? Quaternion.Euler(openEuler) : Quaternion.Euler(closedEuler)));
    }

    System.Collections.IEnumerator AnimateToRotation(Quaternion target)
    {
        Quaternion start = screenTransform.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * openCloseSpeed;
            screenTransform.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        screenTransform.localRotation = target;
        anim = null;
    }

    public bool IsOpen() => isOpen;
}
