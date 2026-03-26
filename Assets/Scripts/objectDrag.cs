using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class objectDrag : MonoBehaviour
{
    public float grabDistance = 3f;
    public float moveSpeed = 15f;
    public float scrollSpeed = 2f;
    public float rotationSpeed = 100f;

    public PlayerController playerController;

    private GameObject grabbedObject;
    private GameObject highlightedObject;
    private AttachablePart currentPart;
    private GameObject previewObject;

    private bool isRotating = false;

    private Rigidbody grabbedRB;

   
    void Start()
    {
        
        if (playerController == null && Camera.main != null)
        {
            playerController = Camera.main.GetComponentInParent<PlayerController>();
        }
    }
    void Update()
    {
        HighlightObject();

        // Grab Object
        if(Input.GetMouseButtonDown(0))
        {
            TryGrab();
        }

        // Release object
        if(Input.GetMouseButtonUp(0))
        {
            Release();
        }

        // Toggle Rotation Mode
        if (grabbedObject != null && Input.GetKeyDown(KeyCode.R))
        {
            isRotating = !isRotating;
        }

        // Move, rotate, scroll distance if grabbing
        if (grabbedObject != null)
        {
            MoveObject();
            if (isRotating)
            {
                if (isRotating)
                {
                    RotateObject();
                }
            }
            ScrollDistance();

            currentPart = grabbedObject.GetComponent<AttachablePart>();
            if (currentPart != null)
            {
                ShowPreview(currentPart);
            }
        }
    }

    void HighlightObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        GameObject newHighlight = null;

        if(Physics.Raycast(ray, out hit, grabDistance))
        {
            if (hit.collider.CompareTag("Draggable"))
                newHighlight = hit.collider.gameObject;
        }
        if (highlightedObject != newHighlight)
        {
           if (highlightedObject != null)
            {
                Outline oldOutline = highlightedObject.GetComponentInChildren<Outline>();
                if (oldOutline != null)
                    oldOutline.enabled = false;
            }
           if (newHighlight != null)
            {
                Outline newOutline = newHighlight.GetComponentInChildren<Outline>();
                if (newOutline != null)
                    newOutline.enabled = true;
            }

           highlightedObject = newHighlight;
        }
    }

    void TryGrab()
    {
        if (highlightedObject == null) return;

        grabbedObject = highlightedObject;
        grabbedRB = grabbedObject.GetComponent<Rigidbody>();

        AttachablePart attachable = grabbedObject.GetComponent<AttachablePart>();
        if (attachable != null)
        {
            attachable.Detach();
        }

        if (grabbedRB != null)
        {
            grabbedRB.useGravity = false;
            //grabbedRB.isKinematic = true;
            grabbedRB.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (playerController != null)
        {
            playerController.canLook = false;
        }    
    }

    void MoveObject()
    {
        if (grabbedRB == null) return;

        Vector3 targetPosition = transform.position + transform.forward * grabDistance;

        Vector3 direction = targetPosition - grabbedRB.position;

        grabbedRB.velocity = direction * moveSpeed;
    }

    void RotateObject()
    {
        
        if (grabbedRB == null) return;

        float rotationStep = rotationSpeed * Time.deltaTime * 1.5f;

        // Horizontal Rotate
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.Keypad6))
        {
            grabbedRB.MoveRotation(grabbedRB.rotation * Quaternion.Euler(0f, rotationStep, 0f));
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Keypad4))
        {
            grabbedRB.MoveRotation(grabbedRB.rotation * Quaternion.Euler(0f, -rotationStep, 0f));
        }

        //Vertical Rotate
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Keypad8))
        {
            grabbedRB.MoveRotation(grabbedRB.rotation * Quaternion.Euler(-rotationStep, 0f, 0f));
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Keypad2))
        {
            grabbedRB.MoveRotation(grabbedRB.rotation * Quaternion.Euler(rotationStep, 0f, 0f));
        }
    }

    void ScrollDistance()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        grabDistance += scroll * scrollSpeed;
        grabDistance = Mathf.Clamp(grabDistance, 1f, 6f);
    }
    void Release()
    {
        if (grabbedObject != null && grabbedRB != null)
        {
            grabbedRB.useGravity = true;
            grabbedRB.isKinematic = false;
        }

        AttachablePart attachable = grabbedObject?.GetComponent<AttachablePart>();
        if (attachable != null)
        {
            attachable.CheckSnap();
        }

        grabbedObject = null;
        grabbedRB = null;

        if (playerController != null)
        {
            playerController.canLook = true;
        }
    }

    void ShowPreview(AttachablePart part)
    {
        float dist = Vector3.Distance(grabbedObject.transform.position, part.attachPoint.position);

        if (dist <= part.snapDistance)
        {
            if (previewObject == null)
            {
                previewObject = Instantiate(grabbedObject, part.attachPoint.position, part.attachPoint.rotation);

                Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    r.material = new Material(r.material);
                    Color c = r.material.color;
                    c.a = 0.5f;
                    r.material.color = c;
                }

                previewObject.GetComponent<Collider>().enabled = false;
            }

            previewObject.transform.position = part.attachPoint.position;
            previewObject.transform.rotation = part.attachPoint.rotation;
        }
        else
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }
        }
    }

}
