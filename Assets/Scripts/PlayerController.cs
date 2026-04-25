using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float crouchSpeed = 2.5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    public Transform cameraHolder;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float standHeight = 1.8f;
    public float crouchSpeedSmooth = 8f;

    [Header("Camera Height")]
    public float standingEyeHeight = 1.6f;
    public float crouchingEyeHeight = 1.0f;

    [Header("Physics / Push Handling")]
    public float gravity = -25f;                    // gravity applied to the character
    public float groundedGravity = -1f;             // small downward force while grounded to keep contact
    public float pushTransfer = 0.5f;               // fraction of rigidbody velocity transferred to player when hit
    public float pushDecay = 4f;                    // how quickly external push velocity decays (higher = faster stop)
    public float maxExternalVelocity = 6f;          // clamp for external velocities

    public bool canLook = true;
    public bool canMove = true;

    float xRotation = 0f;
    CharacterController controller;

    bool isSprinting;
    bool isCrouching;

    // internal physics state
    Vector3 externalVelocity = Vector3.zero; // carries influence from pushed rigidbodies
    float verticalVelocity = 0f;

    // cached camera/FOV
    Camera playerCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // cache camera and FOV
        playerCamera = Camera.main ?? GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
        ApplyExternalVelocityDecay();
    }

    void HandleMouseLook()
    {
        if (!canLook) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        if (controller == null) return;
        if (!canMove) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;

        float speed = walkSpeed;
        if (isSprinting) speed *= sprintMultiplier;
        if (isCrouching) speed = crouchSpeed;

        // horizontal movement in body space
        Vector3 move = (playerBody.right * x + playerBody.forward * z).normalized * speed;

        // handle gravity
        if (controller.isGrounded)
        {
            // keep a small grounded gravity so controller stays snapped to ground
            if (verticalVelocity < 0f)
                verticalVelocity = groundedGravity;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = move + new Vector3(externalVelocity.x, 0f, externalVelocity.z);

        // apply vertical separately
        velocity.y = verticalVelocity + externalVelocity.y;

        // Move the character
        controller.Move(velocity * Time.deltaTime);

        // If after move we are grounded, ensure vertical state is stable
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }
    }

    void HandleCrouch()
    {
        if (controller == null) return;

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }

        float targetHeight = isCrouching ? crouchHeight : standHeight;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * crouchSpeedSmooth
        );

        float targetEyeHeight = isCrouching ? crouchingEyeHeight : standingEyeHeight;

        if (cameraHolder != null)
        {
            Vector3 camPos = cameraHolder.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetEyeHeight, Time.deltaTime * crouchSpeedSmooth);
            cameraHolder.localPosition = camPos;
        }
    }

    void ApplyExternalVelocityDecay()
    {
        if (externalVelocity.sqrMagnitude <= 0.0001f) return;

        // decay horizontal and vertical components separately to feel natural
        externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, Time.deltaTime * pushDecay);

        // clamp to avoid runaway values
        if (externalVelocity.magnitude > maxExternalVelocity)
            externalVelocity = externalVelocity.normalized * maxExternalVelocity;
    }

    // Called when CharacterController hits a Rigidbody
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // If other object has a non-kinematic rigidbody apply a fraction of its velocity to the player
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            // get velocity of the rigidbody (if it has method to expose; fallback to zero if missing)
            Vector3 rbVel = rb.velocity;

            // Transfer the velocity onto the player as an external push influence.
            // Project onto horizontal plane for lateral pushes; keep small vertical component so player can be nudged by upward impacts.
            Vector3 rbVelHorizontal = new Vector3(rbVel.x, 0f, rbVel.z);
            Vector3 transfer = rbVelHorizontal * pushTransfer;

            // Add a small vertical component if the incoming velocity has upward direction
            if (rbVel.y > 0.1f)
                transfer.y = rbVel.y * (pushTransfer * 0.5f);

            // Only add transfer when the hit direction indicates a meaningful impact
            if (transfer.sqrMagnitude > 0.0001f)
            {
                externalVelocity += transfer;

                // keep external velocity in world space relative to player orientation
                // clamp immediately to avoid large lifts
                if (externalVelocity.magnitude > maxExternalVelocity)
                    externalVelocity = externalVelocity.normalized * maxExternalVelocity;
            }

            // Optionally push the rigidbody away a little if the player is moving into it
            Vector3 pushDir = hit.moveDirection;
            // avoid pushing tools / small objects too strongly
            if (!rb.isKinematic && !rb.CompareTag("Tool"))
            {
                try
                {
                    rb.AddForce(pushDir * 0.5f * rb.mass, ForceMode.Impulse);
                }
                catch { }
            }
        }
    }
}