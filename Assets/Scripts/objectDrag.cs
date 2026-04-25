using System.Collections.Generic;
using UnityEngine;

public class objectDrag : MonoBehaviour
{
    [Header("Grab Settings")]
    public float grabDistance = 3f;
    public float moveSpeed = 15f;
    public float scrollSpeed = 2f;

    [Header("Inspect Rotation")]
    public float inspectRotationSpeed = 5f;

    [Header("Keyboard Rotation Mode")]
    public float rotationSpeed = 90f; // degrees per second when in rotation mode

    public PlayerController playerController;

    private GameObject grabbedObject;
    private GameObject highlightedObject;
    private AttachablePart currentPart;
    private GameObject previewObject;

    // main rigidbody reference for the grabbed object (root)
    private Rigidbody grabbedRB;
    // collect all descendant rigidbodies when grabbing complex prefabs (e.g., laptop)
    private Rigidbody[] grabbedChildRBs;
    private bool isInspecting = false;

    // track whether the grabbed object is the laptop root (we move it kinematically)
    private bool grabbedIsLaptop = false;

    // rotation mode state
    private bool rotationMode = false;

    // store previous player state so we can restore it after rotation mode
    private bool prevCanMove = true;
    private bool prevCanLook = true;

    // store original states for child rigidbodies so we restore them exactly on release
    class StoredRBState
    {
        public Rigidbody rb;
        public bool wasKinematic;
        public bool wasUseGravity;
        public CollisionDetectionMode wasCollisionMode;
        public RigidbodyInterpolation wasInterpolation;
        public float wasDrag;
        public float wasAngularDrag;
        public Vector3 wasVelocity;
        public Vector3 wasAngularVelocity;
    }
    private List<StoredRBState> grabbedRBStates;

    // target transforms applied in FixedUpdate via MovePosition/MoveRotation to avoid tunneling
    private Vector3 desiredPosition;
    private Quaternion desiredRotation;
    private bool haveDesiredRotation = false;

    // Grab anchor (physics-driven) to preserve collisions while moving objects
    private GameObject grabAnchor;
    private Rigidbody grabAnchorRB;
    private FixedJoint grabAnchorJoint;
    private bool usingGrabAnchor = false;

    void Start()
    {
        // Ensure we have a valid PlayerController reference
        if (playerController == null)
        {
            if (Camera.main != null)
                playerController = Camera.main.GetComponentInParent<PlayerController>();

            if (playerController == null)
                playerController = FindObjectOfType<PlayerController>();
        }
    }

    void Update()
    {
        HighlightObject();

        if (Input.GetMouseButtonDown(0))
            TryGrab();

        if (Input.GetMouseButtonUp(0))
            Release();

        if (grabbedObject != null)
        {
            // Toggle rotation mode
            if (Input.GetKeyDown(KeyCode.R))
            {
                ToggleRotationMode();
            }

            if (rotationMode)
            {
                HandleKeyboardRotation();
                ScrollDistance(); // allow changing distance while in rotation mode
            }
            else
            {
                isInspecting = Input.GetMouseButton(1); // Right-click to inspect

                // compute targets in Update (visual responsiveness), apply in FixedUpdate to respect physics
                ComputeMoveTarget();

                if (isInspecting)
                    ComputeRotateTarget();

                ScrollDistance();

                currentPart = grabbedObject.GetComponentInParent<AttachablePart>();
                if (currentPart != null)
                    ShowPreview(currentPart);
            }
        }
    }

    void FixedUpdate()
    {
        // Apply the precomputed desiredPosition/desiredRotation using physics-friendly MovePosition/MoveRotation
        if (grabbedObject == null) return;

        if (usingGrabAnchor && grabAnchorRB != null)
        {
            // move the anchor; the connected object will follow via the joint preserving collisions
            try
            {
                if (haveDesiredRotation)
                    grabAnchorRB.MoveRotation(desiredRotation);
                grabAnchorRB.MovePosition(desiredPosition);
            }
            catch
            {
                grabAnchor.transform.position = desiredPosition;
                if (haveDesiredRotation)
                    grabAnchor.transform.rotation = desiredRotation;
            }
        }
        else if (grabbedRB != null)
        {
            // Prefer to move via Rigidbody.MovePosition/MoveRotation every FixedUpdate
            // This keeps physics collision checks active and avoids transform teleport/tunneling.
            try
            {
                if (haveDesiredRotation)
                    grabbedRB.MoveRotation(desiredRotation);

                grabbedRB.MovePosition(desiredPosition);
            }
            catch
            {
                // fallback: if MovePosition/MoveRotation fails for any reason, set transform (less ideal)
                grabbedObject.transform.position = desiredPosition;
                if (haveDesiredRotation)
                    grabbedObject.transform.rotation = desiredRotation;
            }
        }
        else
        {
            // No rigidbody; fallback to transform move (rare)
            grabbedObject.transform.position = desiredPosition;
            if (haveDesiredRotation)
                grabbedObject.transform.rotation = desiredRotation;
        }
    }

    void ComputeMoveTarget()
    {
        if (grabbedRB == null && grabbedObject == null) return;

        Vector3 target = transform.position + transform.forward * grabDistance;

        Vector3 currentPos = (usingGrabAnchor && grabAnchorRB != null) ? grabAnchorRB.position :
                             (grabbedRB != null ? grabbedRB.position : grabbedObject.transform.position);

        desiredPosition = Vector3.Lerp(currentPos, target, Time.deltaTime * moveSpeed);
    }

    void ComputeRotateTarget()
    {
        if (grabbedRB == null && grabbedObject == null) return;

        float mouseX = Input.GetAxis("Mouse X") * inspectRotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * inspectRotationSpeed;

        Transform cam = Camera.main.transform;

        Quaternion rotX = Quaternion.AngleAxis(-mouseY, cam.right);
        Quaternion rotY = Quaternion.AngleAxis(mouseX, cam.up);

        if (usingGrabAnchor && grabAnchorRB != null)
        {
            desiredRotation = rotY * rotX * grabAnchorRB.rotation;
        }
        else if (grabbedIsLaptop)
        {
            desiredRotation = rotY * rotX * grabbedObject.transform.rotation;
        }
        else
        {
            desiredRotation = rotY * rotX * (grabbedRB != null ? grabbedRB.rotation : grabbedObject.transform.rotation);
        }

        haveDesiredRotation = true;
    }

    void ToggleRotationMode()
    {
        if (!rotationMode)
            EnterRotationMode();
        else
            ExitRotationMode();
    }

    void EnterRotationMode()
    {
        rotationMode = true;

        // Ensure playerController exists
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            prevCanMove = playerController.canMove;
            prevCanLook = playerController.canLook;

            playerController.canMove = false;
            playerController.canLook = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Use inspector/default UI text if available; Show() uses the existing text when no parameter is passed.
        RotationModeUI.Instance?.Show();
        Debug.Log("Rotation mode ENTERED");
    }

    void ExitRotationMode()
    {
        rotationMode = false;

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            playerController.canMove = prevCanMove;
            playerController.canLook = prevCanLook;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        RotationModeUI.Instance?.Hide();
            Debug.Log("Rotation mode EXITED");
    }

    void HandleKeyboardRotation()
    {
        if (grabbedObject == null) return;

        // Use raw axes so arrow keys / WASD both work immediately
        float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

        if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f) return;

        float step = rotationSpeed * Time.deltaTime;

        Camera cam = Camera.main;
        Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
        Vector3 worldUp = Vector3.up;

        // Build a delta rotation from inputs (yaw around world up, pitch around camera right)
        Quaternion delta = Quaternion.identity;

        if (Mathf.Abs(h) > 0.01f)
            delta = Quaternion.AngleAxis(h * step, worldUp) * delta;

        if (Mathf.Abs(v) > 0.01f)
            delta = Quaternion.AngleAxis(-v * step, camRight) * delta;

        // Apply rotation via physics-friendly targets so FixedUpdate can MoveRotation.
        if (usingGrabAnchor && grabAnchorRB != null)
        {
            desiredRotation = delta * grabAnchorRB.rotation;
        }
        else if (grabbedRB != null)
        {
            desiredRotation = delta * grabbedRB.rotation;
        }
        else
        {
            // No rigidbody present - fall back to transform rotation but still use desiredRotation so FixedUpdate applies it consistently.
            desiredRotation = delta * grabbedObject.transform.rotation;
        }

        haveDesiredRotation = true;
    }

    void HighlightObject()
    {
        GameObject newHighlight = null;
        Color outlineColor = Color.green;

        var lookObj = InteractionRaycaster.currentLookObject;
        if (lookObj != null)
        {
            // If any parent (or the object itself) is tagged Laptop, highlight the laptop root
            Transform t = lookObj.transform;
            Transform laptopRoot = null;
            while (t != null)
            {
                if (t.CompareTag("Laptop"))
                {
                    laptopRoot = t;
                    break;
                }
                t = t.parent;
            }

            if (laptopRoot != null)
            {
                newHighlight = laptopRoot.gameObject;
                outlineColor = Color.green;
            }
            else if (lookObj.CompareTag("Draggable"))
            {
                AttachablePart potential = lookObj.GetComponentInParent<AttachablePart>();
                PartInfo info = lookObj.GetComponentInParent<PartInfo>();
                ToolItem tItem = lookObj.GetComponentInParent<ToolItem>();

                if (tItem != null && tItem.isEquipped)
                {
                    newHighlight = null;
                }
                else
                {
                    if (info != null)
                    {
                        bool requiresToolMissing = !string.IsNullOrEmpty(info.requiredTool) && (FindObjectOfType<ToolManager>() == null || !FindObjectOfType<ToolManager>().HasTool(info.requiredTool));
                        bool depsUnmet = !info.CanBeRemoved();
                        bool depsTooClose = info.HasNearbyBlockingParts(info.dependencyProximity);

                        outlineColor = (requiresToolMissing || depsUnmet || depsTooClose) ? Color.red : Color.green;
                    }
                    else
                    {
                        outlineColor = Color.green;
                    }

                    if (potential != null)
                        newHighlight = potential.gameObject;
                    else if (info != null)
                        newHighlight = info.gameObject;
                    else
                        newHighlight = lookObj;
                }
            }
        }

        if (highlightedObject == newHighlight) return;

        if (highlightedObject != null)
        {
            Outline prev = highlightedObject.GetComponentInChildren<Outline>();
            if (prev) prev.enabled = false;
        }

        if (newHighlight != null)
        {
            Outline o = newHighlight.GetComponentInChildren<Outline>();
            if (o)
            {
                o.OutlineColor = outlineColor;
                o.enabled = true;
            }
        }

        highlightedObject = newHighlight;
    }

    void TryGrab()
    {
        if (highlightedObject == null) return;

        grabbedIsLaptop = false;
        grabbedChildRBs = null;
        grabbedRB = null;
        grabbedRBStates = null;

        if (highlightedObject.CompareTag("Laptop"))
        {
            grabbedObject = highlightedObject;

            // Prefer root Rigidbody. If none, fall back to child RB.
            grabbedRB = grabbedObject.GetComponent<Rigidbody>() ?? grabbedObject.GetComponentInChildren<Rigidbody>();
            // collect all Rigidbodies in the laptop hierarchy
            grabbedChildRBs = grabbedObject.GetComponentsInChildren<Rigidbody>();

            grabbedIsLaptop = (grabbedObject != null && grabbedRB != null);
        }
        else
        {
            grabbedObject = highlightedObject;
            grabbedRB = grabbedObject.GetComponentInParent<Rigidbody>();

            AttachablePart attachableCheck = grabbedObject.GetComponentInParent<AttachablePart>();
            PartInfo partInfo = grabbedObject.GetComponentInParent<PartInfo>();

            if (attachableCheck != null && attachableCheck.IsAttached)
            {
                grabbedObject = null;
                grabbedRB = null;
                return;
            }

            if (partInfo != null)
            {
                if (!partInfo.CanBeRemoved() || partInfo.HasNearbyBlockingParts(partInfo.dependencyProximity))
                {
                    grabbedObject = null;
                    grabbedRB = null;
                    return;
                }
            }

            ToolItem tool = grabbedObject.GetComponentInParent<ToolItem>();
            if (tool != null && tool.isEquipped)
            {
                grabbedObject = null;
                grabbedRB = null;
                return;
            }
        }

        if (grabbedRB != null)
        {
            if (grabbedIsLaptop && grabbedChildRBs != null && grabbedChildRBs.Length > 0)
            {
                // store states for all descendant rigidbodies and make non-root children kinematic during the drag
                grabbedRBStates = new List<StoredRBState>(grabbedChildRBs.Length);
                foreach (var rb in grabbedChildRBs)
                {
                    if (rb == null) continue;

                    grabbedRBStates.Add(new StoredRBState
                    {
                        rb = rb,
                        wasKinematic = rb.isKinematic,
                        wasUseGravity = rb.useGravity,
                        wasCollisionMode = rb.collisionDetectionMode,
                        wasInterpolation = rb.interpolation,
                        wasDrag = rb.drag,
                        wasAngularDrag = rb.angularDrag,
                        wasVelocity = rb.velocity,
                        wasAngularVelocity = rb.angularVelocity
                    });

                    // Make non-root children kinematic for drag to avoid internal dynamics; root rb should remain dynamic
                    if (rb != grabbedRB)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.WakeUp();
                    }
                }

                // Ensure root stays dynamic so joint / physics can resolve collisions correctly
                grabbedRB.isKinematic = false;
                grabbedRB.useGravity = false;
                grabbedRB.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                grabbedRB.interpolation = RigidbodyInterpolation.Interpolate;
                grabbedRB.velocity = Vector3.zero;
                grabbedRB.angularVelocity = Vector3.zero;
            }
            else
            {
                grabbedRB.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                grabbedRB.interpolation = RigidbodyInterpolation.Interpolate;

                grabbedRB.isKinematic = false;
                grabbedRB.useGravity = false;
                grabbedRB.velocity = Vector3.zero;
                grabbedRB.angularVelocity = Vector3.zero;
            }

            // create physics grab anchor and joint to preserve collisions
            CreateGrabAnchor(grabbedRB.position);
            AttachGrabJoint();
        }

        // initialize desired transform targets so FixedUpdate has sensible starting values
        if (grabbedRB != null)
        {
            desiredPosition = grabbedRB.position;
            desiredRotation = grabbedRB.rotation;
            haveDesiredRotation = true;
        }
        else if (grabbedObject != null)
        {
            desiredPosition = grabbedObject.transform.position;
            desiredRotation = grabbedObject.transform.rotation;
            haveDesiredRotation = true;
        }

        // ensure playerController reference
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            // keep camera look enabled while dragging unless in rotation mode
            playerController.canLook = !rotationMode;
            playerController.canMove = !rotationMode;
        }
    }

    void CreateGrabAnchor(Vector3 atPosition)
    {
        DestroyGrabAnchor();

        grabAnchor = new GameObject("GrabAnchor");
        grabAnchor.transform.position = atPosition;
        grabAnchorRB = grabAnchor.AddComponent<Rigidbody>();

        // Make anchor kinematic and collisionless; it drives the joint so the grabbed body follows physically.
        grabAnchorRB.isKinematic = true;
        grabAnchorRB.useGravity = false;
        grabAnchorRB.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        grabAnchorRB.interpolation = RigidbodyInterpolation.Interpolate;

        // optionally mark as ignore raycasts layer, etc. (left default here)
        DontDestroyOnLoad(grabAnchor);
        usingGrabAnchor = false; // will set true once joint attached
    }

    void AttachGrabJoint()
    {
        if (grabAnchor == null || grabAnchorRB == null || grabbedRB == null) return;

        // Attach a FixedJoint on the anchor that connects to the grabbed rigidbody.
        grabAnchorJoint = grabAnchor.AddComponent<FixedJoint>();
        grabAnchorJoint.connectedBody = grabbedRB;
        grabAnchorJoint.breakForce = float.PositiveInfinity;
        grabAnchorJoint.breakTorque = float.PositiveInfinity;

        usingGrabAnchor = true;
    }

    void DestroyGrabAnchor()
    {
        if (grabAnchorJoint != null)
        {
            Destroy(grabAnchorJoint);
            grabAnchorJoint = null;
        }

        if (grabAnchor != null)
        {
            Destroy(grabAnchor);
            grabAnchor = null;
        }

        grabAnchorRB = null;
        usingGrabAnchor = false;
    }

    void ScrollDistance()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        grabDistance = Mathf.Clamp(grabDistance + scroll * scrollSpeed, 1f, 6f);
    }

    void Release()
    {
        if (grabbedObject == null) return;

        AttachablePart attachable = grabbedObject.GetComponentInParent<AttachablePart>();

        bool snapped = false;

        if (attachable != null)
            snapped = attachable.CheckSnap();

        // detach the anchor first so physics doesn't try to pull things while we restore states
        DestroyGrabAnchor();

        // Use stored states where available to restore exactly what was before grab.
        if (!snapped)
        {
            if (grabbedIsLaptop)
            {
                // If we stored states for descendants, restore them individually.
                if (grabbedRBStates != null && grabbedRBStates.Count > 0)
                {
                    // First, resolve penetration using the root transform (doesn't force child RBs dynamic)
                    ResolvePenetrationAndSettle(grabbedObject);

                    // Restore each RB's original state. We zero small velocities to avoid jitter,
                    // but keep kinematic flags as they originally were so attached parts remain kinematic.
                    foreach (var state in grabbedRBStates)
                    {
                        if (state == null || state.rb == null) continue;

                        var rb = state.rb;
                        rb.isKinematic = state.wasKinematic;
                        rb.useGravity = state.wasUseGravity;
                        rb.collisionDetectionMode = state.wasCollisionMode;
                        rb.interpolation = state.wasInterpolation;
                        rb.drag = state.wasDrag;
                        rb.angularDrag = state.wasAngularDrag;

                        // Zero tiny velocities to prevent explosion; large intended velocities would be restored by game logic if needed.
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;

                        // If RB is dynamic, wake it so physics continues smoothly.
                        if (!rb.isKinematic)
                        {
                            rb.WakeUp();
                        }
                    }

                    grabbedRBStates = null;
                }
                else if (grabbedRB != null)
                {
                    // fallback single RB case
                    grabbedRB.isKinematic = false;
                    grabbedRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    grabbedRB.interpolation = RigidbodyInterpolation.Interpolate;

                    grabbedRB.drag = Mathf.Max(grabbedRB.drag, 1f);
                    grabbedRB.angularDrag = Mathf.Max(grabbedRB.angularDrag, 0.5f);

                    ResolvePenetrationAndSettle(grabbedObject);

                    grabbedRB.useGravity = true;
                    grabbedRB.velocity = Vector3.down * 0.5f;
                    grabbedRB.angularVelocity = Vector3.zero;
                    grabbedRB.ResetInertiaTensor();
                    grabbedRB.WakeUp();
                }
            }
            else
            {
                if (grabbedRB != null)
                {
                    grabbedRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    grabbedRB.useGravity = true;
                    grabbedRB.velocity = Vector3.zero;
                }
            }
        }

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        // ensure rotation mode exits and UI hides when user drops object
        if (rotationMode)
            ExitRotationMode();

        grabbedObject = null;
        grabbedRB = null;
        grabbedChildRBs = null;
        grabbedIsLaptop = false;
        haveDesiredRotation = false;
    }

    // ResolvePenetrationAndSettle:
    // Attempts to resolve any collider intersections between the released object (usually the laptop root)
    // and the rest of the world by computing penetration vectors and moving the object out of overlap.
    // Conservative, iterative approach that is inexpensive and robust for typical scene geometry.
    void ResolvePenetrationAndSettle(GameObject root)
    {
        if (root == null) return;

        // collect colliders that belong to the released object
        Collider[] myColliders = root.GetComponentsInChildren<Collider>();
        if (myColliders == null || myColliders.Length == 0) return;

        // Prefer a dedicated root Rigidbody if available (LaptopTools ensures one on root if used).
        Rigidbody rb = root.GetComponent<Rigidbody>() ?? root.GetComponentInChildren<Rigidbody>();

        const int maxIterations = 8;
        const float eps = 0.0005f;
        const float maxPerIterationMove = 0.12f; // reduced to avoid overshoot/jitter

        // Use layer mask to consider all layers
        int layerMask = ~0;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            Vector3 accumulatedCorrection = Vector3.zero;
            bool anyPenetration = false;

            foreach (var col in myColliders)
            {
                if (col == null || !col.enabled) continue;

                // quick AABB query to find nearby colliders
                Vector3 halfExtents = col.bounds.extents + Vector3.one * 0.01f;
                Collider[] candidates = Physics.OverlapBox(col.bounds.center, halfExtents, col.transform.rotation, layerMask, QueryTriggerInteraction.Ignore);

                foreach (var other in candidates)
                {
                    if (other == null) continue;
                    if (other.transform.IsChildOf(root.transform)) continue;
                    if (other.isTrigger) continue;

                    // compute penetration
                    if (Physics.ComputePenetration(
                        col, col.transform.position, col.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 direction, out float distance))
                    {
                        if (distance <= eps) continue;

                        // Special-case floor/environment to prefer upward correction to avoid lateral push that makes object slide/float
                        if (other.CompareTag("Floor") || other.CompareTag("Environment"))
                        {
                            // Keep only the upward component if it helps; otherwise fallback to full direction
                            Vector3 upComp = Vector3.Project(direction, Vector3.up);
                            if (upComp.sqrMagnitude > 0.0001f && Vector3.Dot(upComp.normalized, Vector3.up) > 0f)
                            {
                                accumulatedCorrection += upComp.normalized * distance;
                            }
                            else
                            {
                                // if direction doesn't have a useful upward component, move along direction but reduced
                                accumulatedCorrection += direction * distance * 0.8f;
                            }
                        }
                        else
                        {
                            // For other contacts, add the separation vector but scale down to avoid big jumps
                            accumulatedCorrection += direction * distance;
                        }

                        anyPenetration = true;
                    }
                }
            }

            if (!anyPenetration)
                break;

            // Clamp per-iteration correction to avoid overshoot / floating
            Vector3 correction = Vector3.ClampMagnitude(accumulatedCorrection, maxPerIterationMove);

            if (correction.sqrMagnitude <= 1e-6f)
                break;

            // Apply correction via Rigidbody when possible (keeps physics continuity)
            if (rb != null && !rb.isKinematic)
            {
                // Use position update here; it's small, iterative and performed immediately during Release.
                rb.position += correction;
                rb.WakeUp();
            }
            else
            {
                root.transform.position += correction;
            }
        }

        // After resolving, ensure rigidbody (if any) is stable
        if (rb != null)
        {
            // Zero small velocities that cause bouncing/floating
            if (rb.velocity.sqrMagnitude < 0.01f)
                rb.velocity = Vector3.zero;

            if (rb.angularVelocity.sqrMagnitude < 0.01f)
                rb.angularVelocity = Vector3.zero;

            // sync transform and wake
            rb.position = root.transform.position;
            rb.rotation = root.transform.rotation;
            rb.WakeUp();
        }
    }

    void ShowPreview(AttachablePart part)
    {
        if (part.attachPoint == null || grabbedObject == null) return;

        float dist = Vector3.Distance(grabbedObject.transform.position, part.attachPoint.position);

        if (dist <= part.snapDistance)
        {
            if (previewObject == null)
            {
                previewObject = Instantiate(grabbedObject);

                Destroy(previewObject.GetComponent<Rigidbody>());
                Destroy(previewObject.GetComponent<AttachablePart>());

                foreach (var col in previewObject.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                foreach (var r in previewObject.GetComponentsInChildren<Renderer>())
                {
                    Material m = new Material(r.material);
                    Color c = m.color;
                    c.a = 0.4f;
                    m.color = c;
                    r.material = m;
                }
            }

            previewObject.transform.position = part.attachPoint.position;
            previewObject.transform.rotation = part.attachPoint.rotation;
        }
        else
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
        }
    }

    public void ForceRelease()
    {
        // If we are forcing a release while holding RB states, restore them to avoid leaving parts dynamic
        DestroyGrabAnchor();

        if (grabbedRBStates != null)
        {
            foreach (var state in grabbedRBStates)
            {
                if (state == null || state.rb == null) continue;
                state.rb.isKinematic = state.wasKinematic;
                state.rb.useGravity = state.wasUseGravity;
                state.rb.collisionDetectionMode = state.wasCollisionMode;
                state.rb.interpolation = state.wasInterpolation;
                state.rb.drag = state.wasDrag;
                state.rb.angularDrag = state.wasAngularDrag;
                state.rb.velocity = Vector3.zero;
                state.rb.angularVelocity = Vector3.zero;
                if (!state.rb.isKinematic) state.rb.WakeUp();
            }
            grabbedRBStates = null;
        }

        grabbedObject = null;
        grabbedRB = null;
        grabbedChildRBs = null;
        grabbedIsLaptop = false;
        rotationMode = false;
        haveDesiredRotation = false;

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        if (playerController != null)
        {
            playerController.canLook = true;
            playerController.canMove = true;
        }
    }
}