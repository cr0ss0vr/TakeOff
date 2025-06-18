using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float lookSensitivity = 3f;
    [SerializeField] private float rollSpeed = 45f; // degrees per second

    [SerializeField] private Transform cameraPivot;

    [SerializeField] private PlayerGroundCheck groundCheck;
    private bool grounded; 
    private bool freeFlightActive = false;

    [SerializeField] private InputActionAsset inputActions;  // Assign in Inspector

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction verticalAction;
    private InputAction rollAction;
    private float pitch = 0f;

    private float jumpHoldTime = 0f;
    private bool jumpedThisHold = false;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        var playerActions = inputActions.FindActionMap("Player");
        moveAction = playerActions.FindAction("Move");
        lookAction = playerActions.FindAction("Look");
        verticalAction = playerActions.FindAction("Vertical");
        rollAction = playerActions.FindAction("Roll");

        moveAction.Enable();
        lookAction.Enable();
        verticalAction.Enable();
        rollAction.Enable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        grounded = groundCheck.IsGrounded;

        if (jumpHoldTime >= 3f)
        {
            if (!freeFlightActive)
            {
                freeFlightActive = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rollAction.Enable();
            }
        }
        else if (freeFlightActive && grounded)
        {
            freeFlightActive = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rollAction.Disable();
        }

        HandleRotation();
    }


    private void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float verticalInput = verticalAction.ReadValue<float>();

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        // Jump hold logic
        if (verticalInput >= 1f)
        {
            jumpHoldTime += Time.fixedDeltaTime;

            if (!jumpedThisHold && grounded)
            {
                JumpImpulse();
                jumpedThisHold = true;
            }
        }
        else
        {
            jumpHoldTime = 0f;
            jumpedThisHold = false;
        }

        Vector3 desiredVelocity;

        if (grounded && !freeFlightActive)
        {
            // Flatten movement on XZ plane, preserve current vertical velocity for gravity
            forward.y = 0f;
            right.y = 0f;

            desiredVelocity = (right.normalized * moveInput.x + forward.normalized * moveInput.y) * movementSpeed;
            // Preserve vertical velocity (gravity/jump)
            desiredVelocity.y = rb.linearVelocity.y;
        }
        else if (freeFlightActive)
        {
            // free flight: allow full 3D movement
            desiredVelocity = (right.normalized * moveInput.x + forward.normalized * moveInput.y + up.normalized * verticalInput) * movementSpeed;
        }
        else
        {
            // Flatten movement on XZ plane, preserve current vertical velocity for gravity
            forward.y = 0f;
            right.y = 0f;

            // in air after jump impulse but before free flight, allow horizontal movement & falling
            desiredVelocity = (right.normalized * moveInput.x + forward.normalized * moveInput.y) * movementSpeed;
            desiredVelocity.y = rb.linearVelocity.y;
        }

        // Apply velocity
        rb.linearVelocity = desiredVelocity;
    }

    void JumpImpulse()
    {
        float jumpForce = 5f; // tweak to your needs

        // Reset vertical velocity and add jump force
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    void HandleRotation()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float rollInput = rollAction.ReadValue<float>();
        float pitchDelta = lookInput.y * lookSensitivity * Time.deltaTime;
        float yawDelta = lookInput.x * lookSensitivity * Time.deltaTime;

        // Yaw (left/right) always rotates the player
        transform.Rotate(Vector3.up, lookInput.x * lookSensitivity * Time.deltaTime);

        if(!freeFlightActive)
        {
            // Grounded mode – rotate player on Y axis only
            transform.Rotate(Vector3.up, yawDelta);

            // Clamp and apply pitch to camera pivot only
            pitch -= pitchDelta;
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // Lock transform's rotation to Y only (reset X and Z)
            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
        }
        else
        {
            // Free flight rotation with full pitch and roll
            Vector3 pivotPoint = cameraPivot.position;
            Vector3 pitchAxis = cameraPivot.right;
            float pitchAngle = -pitchDelta;
            transform.RotateAround(pivotPoint, pitchAxis, pitchAngle);

            float rollAngle = rollInput * rollSpeed * Time.deltaTime;
            Vector3 rollAxis = cameraPivot.forward;
            transform.RotateAround(pivotPoint, rollAxis, rollAngle);
        }
    }
}
