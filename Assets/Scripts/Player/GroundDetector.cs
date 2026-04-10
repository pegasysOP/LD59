using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; }

    public LayerMask playerMask;

    private void FixedUpdate()
    {
        DetectGround();
    }

    private void DetectGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, transform.localScale.x, ~playerMask))
        {
            // if incline is too steep don't count as grounded
            if (Vector3.Angle(hit.normal, Vector3.up) < 45) // 45 degrees
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
                return;
            }
        }

        GroundNormal = Vector3.up;
        IsGrounded = false;
    }
}
