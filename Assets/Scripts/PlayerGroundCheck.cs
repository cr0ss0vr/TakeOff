using UnityEngine;

public class PlayerGroundCheck : MonoBehaviour
{
    public bool IsGrounded { get; private set; }

    [SerializeField] private float rayDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer; // Assign your ground layer in Inspector

    void Update()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        IsGrounded = Physics.Raycast(ray, rayDistance, groundLayer);

        // Optional: for debugging in Scene view
        Debug.DrawRay(transform.position, Vector3.down * rayDistance, IsGrounded ? Color.green : Color.red);
    }
}
