using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerControlRemote : NetworkBehaviour
{
    [Header("Smoothing Settings")]
    [SerializeField] private float positionSmoothTime = 0.1f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private Transform cameraPivot;

    private Vector3 velocity;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetPitch;
    private float currentPitch;

    private void Awake()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        targetPitch = cameraPivot.localRotation.eulerAngles.x;
    }

    public override void OnNetworkSpawn()
    {
    }

    private void Update()
    {
        if (!IsSpawned) return;
        if (IsOwner) return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            positionSmoothTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime / rotationSmoothTime
        );

        currentPitch = Mathf.LerpAngle(
            currentPitch,
            targetPitch,
            Time.deltaTime / rotationSmoothTime
        );

        cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    public void SetTargetState(Vector3 position, Quaternion rotation, float pitch)
    {
        targetPosition = position;
        targetRotation = rotation;
        targetPitch = pitch;
    }
}
