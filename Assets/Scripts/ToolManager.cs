using UnityEngine;
using System.Collections;

public class ToolManager : MonoBehaviour
{
    public Transform holdPoint;

    private GameObject equippedTool;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryEquip();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Unequip();
        }
    }

    void TryEquip()
    {
        if (equippedTool != null) return;

        GameObject target = InteractionRaycaster.currentLookObject;
        if (target == null) return;

        ToolItem tool = target.GetComponent<ToolItem>();
        if (tool == null) return;

        Equip(target, tool);
    }
    
    void Equip(GameObject toolObj, ToolItem tool)
    {

        objectDrag drag = FindObjectOfType<objectDrag>();
        if (drag != null)
        {
            drag.ForceRelease();
        }

        equippedTool = toolObj;
        tool.isEquipped = true;

        Rigidbody rb = toolObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // keep collision mode for kinematic tools
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        Collider col = toolObj.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        toolObj.transform.SetParent(holdPoint);
        toolObj.transform.localPosition = Vector3.zero;
        toolObj.transform.localRotation = Quaternion.identity;

        AttachablePart ap = toolObj.GetComponent<AttachablePart>();
        if (ap != null)
            ap.isTool = true;
    }

    void Unequip()
    {

        if (equippedTool == null) return;

        GameObject toolObj = equippedTool;

        ToolItem tool = toolObj.GetComponent<ToolItem>();
        if (tool != null)
            tool.isEquipped = false;

        AttachablePart attachable = toolObj.GetComponent<AttachablePart>();
        if (attachable != null)
            attachable.Detach();

        AttachablePart ap = toolObj.GetComponent<AttachablePart>();
        if (ap != null)
        {
            ap.isTool = true;
        }

        toolObj.transform.SetParent(null);

        Transform cam = Camera.main.transform;

        Vector3 safePosition =
            cam.position +
            cam.forward * 2.5f +
            Vector3.down * 0.5f;

        toolObj.transform.position = safePosition;
        toolObj.transform.rotation = Quaternion.identity;

        Rigidbody rb = toolObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // ensure continuous dynamic collision on drop to avoid tunneling
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        Collider col = toolObj.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        toolObj.tag = "Untagged";

        StartCoroutine(FinalizeDrop(toolObj, col));

        equippedTool = null;
    }

    IEnumerator FinalizeDrop(GameObject obj, Collider col)
    {
        yield return new WaitForSeconds(0.2f);

        if (col != null)
            col.enabled = true;

        obj.tag = "Draggable";
    }

    public bool HasTool(string toolName)
    {
        if (equippedTool == null) return false;

        ToolItem tool = equippedTool.GetComponent<ToolItem>();
        return tool != null && tool.toolName == toolName;
    }
}