using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.Windows;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
public class PlayerControlLocal : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private PlayerControlRemote playerControlRemote;
    [SerializeField] private PlayerGroundCheck groundCheck;

    [Header("Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float freeFlightMultiplier = 2f;
    [SerializeField] private float initialDamping = 0.5f;
    [SerializeField] private float spaceMovementDamping = 1f;
    [SerializeField] private float surfaceMovementDamping = 5f;
    [SerializeField] private float lookSensitivity = 3f;
    [SerializeField] private float rollSpeed = 45f;
    [SerializeField] private float pitchResetSpeed = 2f;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction verticalAction;
    private InputAction rollAction;
    private InputAction interactAction;
    private Rigidbody rb;

    private Vector2 moveInput;
    private float verticalInput;
    private float yawInput;
    private float pitchInput;
    private float rollInput;
    private bool jumpPressed;
    private bool jumpedThisHold;
    private bool IsFreeFlightActive;
    private bool IsGrounded;
    private readonly List<Debris> nearbyDebrisList = new();
    private float interactStartTime = -1f;

    private Quaternion freeFlightTargetRotation;
    private bool isAligningForFreeFlight = false;
    private float freeFlightAlignSpeed = 2f;
    private bool resettingPitch = false;

    private float pitch = 0f;


    public override void OnNetworkSpawn()
    {
        var cam = GetComponentInChildren<Camera>();
        var listener = GetComponentInChildren<AudioListener>();

        if (cam)
            if (IsOwner)
                cam.enabled = true;
            else
                cam.enabled = false;

        if (listener)
            if (IsOwner)
                listener.enabled = true;
            else
                listener.enabled = false;

        if (!IsOwner)
            return;

        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var playerActions = inputActions.FindActionMap("Player");
        moveAction = playerActions.FindAction("Move");
        lookAction = playerActions.FindAction("Look");
        verticalAction = playerActions.FindAction("Vertical");
        rollAction = playerActions.FindAction("Roll");
        interactAction = playerActions.FindAction("Interact");

        if (interactAction != null)
        {
            interactAction.started += _ => interactStartTime = Time.time;
            interactAction.canceled += _ => HandleInteractRelease();
        }

        moveAction.Enable();
        lookAction.Enable();
        verticalAction.Enable();
        rollAction.Enable();
        interactAction.Enable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        ExitFreeFlight();

    }

    private void Update()
    {
        if (!IsOwner) return;

        IsGrounded = groundCheck.IsGrounded;

        ReadInputs();

        if (IsGrounded && IsFreeFlightActive)
            ExitFreeFlight();

        if (isAligningForFreeFlight)
            AlignToCameraSmoothly();

        HandleCameraLook();
        HandleMovement();
        HandleJump();

        if (IsFreeFlightActive)
        {
            HandleRoll();
        }
        
        SendTransformToServerRpc(transform.position, transform.rotation, pitch);
    }

    public void ReadInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        verticalInput = verticalAction.ReadValue<float>();

        Vector2 look = lookAction.ReadValue<Vector2>();
        yawInput = look.x;
        pitchInput = look.y;

        rollInput = rollAction.ReadValue<float>();
        jumpPressed = verticalInput >= 1f;
    }

    private void HandleJump()
    {
        if (jumpPressed && !jumpedThisHold)
        {
            if (IsGrounded)
                JumpImpulse();
            else if (!IsFreeFlightActive)
                EnterFreeFlight();
            jumpedThisHold = true;
        }

        if (!jumpPressed)
            jumpedThisHold = false;
    }

    private void HandleCameraLook()
    {
        if (IsFreeFlightActive)
        {
            float yawDelta = yawInput * lookSensitivity * Time.deltaTime * 2f;
            float pitchDelta = -pitchInput * lookSensitivity * Time.deltaTime * 2f;

            Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, transform.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, transform.right);

            Quaternion targetRotation = yawRotation * pitchRotation * rb.rotation;
            rb.MoveRotation(targetRotation);
        }
        else
        {
            pitch -= pitchInput * lookSensitivity * Time.deltaTime * 2f;
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);

            Quaternion yawRotation = Quaternion.AngleAxis(yawInput * lookSensitivity * Time.deltaTime * 2f, Vector3.up);
            rb.MoveRotation(yawRotation * rb.rotation);

            if (resettingPitch)
            {
                pitch = Mathf.MoveTowards(pitch, 0f, pitchResetSpeed * Time.deltaTime);
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
                if (Mathf.Abs(pitch) < 0.01f)
                {
                    pitch = 0f;
                    resettingPitch = false;
                }
            }
        }
    }

    private void HandleMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        Vector3 desiredVelocity;

        if (IsGrounded && !IsFreeFlightActive)
        {
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            desiredVelocity = (right * moveInput.x + forward * moveInput.y) * movementSpeed;
            desiredVelocity.y = rb.linearVelocity.y;

            rb.linearVelocity = desiredVelocity;
        }
        else if (IsFreeFlightActive)
        {
            Vector3 direction = (right * moveInput.x + forward * moveInput.y + up * verticalInput).normalized;
            rb.AddForce(direction * movementSpeed * freeFlightMultiplier, ForceMode.Acceleration);
        }
        else
        {
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            desiredVelocity = (right * moveInput.x + forward * moveInput.y) * movementSpeed;
            desiredVelocity.y = rb.linearVelocity.y;

            rb.linearVelocity = desiredVelocity;
        }
    }

    private void HandleRoll()
    {
        if (Mathf.Abs(rollInput) > 0.01f)
        {
            Quaternion rollRotation = Quaternion.AngleAxis(rollInput * rollSpeed * Time.deltaTime, transform.forward);
            rb.MoveRotation(rollRotation * rb.rotation);
        }
    }

    private void JumpImpulse()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0;
        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * 5f, ForceMode.VelocityChange);
    }

    private void EnterFreeFlight()
    {
        IsFreeFlightActive = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.linearDamping = initialDamping * spaceMovementDamping;
        rb.constraints = RigidbodyConstraints.None;

        pitch = 0f;
        resettingPitch = false;
        freeFlightTargetRotation = Quaternion.LookRotation(cameraPivot.forward, transform.up);
        isAligningForFreeFlight = true;
    }

    private void ExitFreeFlight()
    {
        IsFreeFlightActive = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.linearDamping = initialDamping * surfaceMovementDamping;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.rotation = Quaternion.Euler(0f, rb.rotation.eulerAngles.y, 0f);
        isAligningForFreeFlight = false;
    }

    private void AlignToCameraSmoothly()
    {
        rb.rotation = Quaternion.RotateTowards(rb.rotation, freeFlightTargetRotation, freeFlightAlignSpeed * Time.deltaTime);
        cameraPivot.localRotation = Quaternion.RotateTowards(cameraPivot.localRotation, Quaternion.identity, freeFlightAlignSpeed * Time.deltaTime);

        if (Quaternion.Angle(rb.rotation, freeFlightTargetRotation) < 0.1f &&
            Quaternion.Angle(cameraPivot.localRotation, Quaternion.identity) < 0.1f)
        {
            isAligningForFreeFlight = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (other.TryGetComponent<Debris>(out var debris) && !nearbyDebrisList.Contains(debris))
            nearbyDebrisList.Add(debris);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;
        if (other.TryGetComponent<Debris>(out var debris))
            nearbyDebrisList.Remove(debris);
    }

    private void HandleInteractRelease()
    {
        float held = Time.time - interactStartTime;
        if (nearbyDebrisList.Count == 0) return;

        if (held < 0.3f)
            TryCollectDebris(nearbyDebrisList[0]);
        else
            foreach (var debris in nearbyDebrisList.ToArray())
                TryCollectDebris(debris);

        interactStartTime = -1f;
    }

    private void TryCollectDebris(Debris debris)
    {
        if (!debris) return;

        debris.Collect(transform);
        nearbyDebrisList.Remove(debris);
    }

    [ServerRpc]
    private void SendTransformToServerRpc(Vector3 position, Quaternion rotation, float pitch, ServerRpcParams rpcParams = default)
    {
        ApplyRemoteTransformClientRpc(position, rotation, pitch);
    }

    [ClientRpc]
    private void ApplyRemoteTransformClientRpc(Vector3 position, Quaternion rotation, float pitch, ClientRpcParams rpcParams = default)
    { 
        if (IsOwner) return; // Only remote players apply this

        playerControlRemote.SetTargetState(position, rotation, pitch);
    }

}
