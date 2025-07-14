using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkRigidbody), typeof(Rigidbody))]
public class Debris : NetworkBehaviour
{
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float springForce = 50f;
    [SerializeField] private float damping = 5f;

    private bool collected = false;
    private Transform followTarget;
    private Rigidbody rb;

    private NetworkVariable<NetworkObjectReference> followTargetRef = new(writePerm: NetworkVariableWritePermission.Server);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            rb.isKinematic = true; // Non-servers do not simulate physics
        }

        followTargetRef.OnValueChanged += OnFollowTargetChanged;

        // Ensure follow target is resolved on clients after spawn
        if (followTargetRef.Value.TryGet(out var netObj))
        {
            followTarget = netObj.transform;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || followTarget == null) return;

        Vector3 toTarget = followTarget.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance > followDistance)
        {
            Vector3 direction = toTarget.normalized;
            Vector3 spring = direction * (distance - followDistance) * springForce;
            Vector3 damp = -rb.linearVelocity * damping;

            rb.AddForce(spring + damp);
        }
    }

    public void Collect(Transform playerTransform)
    {
        if (collected) return;

        var playerNetObj = playerTransform.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        collected = true;
        followTargetRef.Value = playerNetObj;
        followTarget = playerTransform;
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void OnFollowTargetChanged(NetworkObjectReference oldRef, NetworkObjectReference newRef)
    {
        if (newRef.TryGet(out var netObj))
        {
            followTarget = netObj.transform;
        }
    }
}
