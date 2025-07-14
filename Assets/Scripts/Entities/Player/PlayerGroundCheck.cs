using System.Text.RegularExpressions;
using UnityEngine;

public class PlayerGroundCheck : MonoBehaviour
{
    [SerializeField] private CapsuleCollider playerCollider;

    public bool IsGrounded { get; private set; }
    public Vector3 SurfaceNormal { get; private set; } = Vector3.up;

    [Header("Settings")]
    [SerializeField] private float checkDistance = 0.3f;
    [SerializeField] private float sphereRadius = 0.25f;

    [Tooltip("Layers to ignore during ground checks (e.g. Player, Debris, Machines)")]
    [SerializeField] private LayerMask ignoreLayers;

    private CapsuleCollider capsule;

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
    }

    private void FixedUpdate()
    {
        Vector3 down = -transform.up;
        Vector3 origin = GetCapsuleBottomWorldPosition(down) + down * 0.05f;

        RaycastHit hit;

        // Invert ignore mask to create a whitelist of everything else
        int groundMask = ~ignoreLayers;

        if (Physics.SphereCast(origin, sphereRadius, down, out hit, checkDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            IsGrounded = true;
            SurfaceNormal = hit.normal;
            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
        else
        {
            IsGrounded = false;
            SurfaceNormal = transform.up;
        }

        Debug.DrawRay(origin, down * checkDistance, IsGrounded ? Color.green : Color.red);
    }

    private Vector3 GetCapsuleBottomWorldPosition(Vector3 down)
    {
        Vector3 worldCenter = transform.TransformPoint(capsule.center);
        float bottomOffset = (capsule.height * 0.5f) - capsule.radius;
        return worldCenter + down * bottomOffset;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || capsule == null) return;

        Vector3 down = -transform.up;
        Vector3 origin = GetCapsuleBottomWorldPosition(down) + down * 0.05f;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin + down * checkDistance, sphereRadius);
    }
#endif
}