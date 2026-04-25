using UnityEditor;
using UnityEngine;
using System.Linq;

public static class LaptopTools
{
    [MenuItem("Tools/Laptop/Center Pivot & Ensure Rigidbody", false, 100)]
    public static void CenterPivotAndEnsureRigidbody()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("Select the laptop root GameObject first.");
            return;
        }

        GameObject root = Selection.activeGameObject;

        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("No Renderers found under selected GameObject.");
            return;
        }

        Bounds total = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            total.Encapsulate(renderers[i].bounds);

        Vector3 worldCenter = total.center;

        Undo.RegisterCompleteObjectUndo(root.transform, "Center Laptop Pivot");
        Vector3 delta = worldCenter - root.transform.position;

        foreach (Transform child in root.transform)
        {
            Undo.RecordObject(child, "Preserve Child Position");
            child.position -= delta;
        }

        root.transform.position = worldCenter;

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Undo.AddComponent<Rigidbody>(root);
            rb = root.GetComponent<Rigidbody>();
        }

        Undo.RecordObject(rb, "Configure Rigidbody");
        rb.mass = Mathf.Max(1f, rb.mass);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;

        if (root.tag != "Laptop")
        {
            Undo.RecordObject(root, "Set Laptop Tag");
            root.tag = "Laptop";
        }

        Debug.Log($"Centered pivot of '{root.name}' to {worldCenter} and ensured Rigidbody.");
    }
}
