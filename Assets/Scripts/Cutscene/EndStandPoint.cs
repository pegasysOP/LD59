using UnityEngine;

public class EndStandPoint : MonoBehaviour
{
    [Tooltip("Max distance to raycast downward to find the floor.")]
    public float groundRaycastDistance = 10f;
    [Tooltip("Layers considered floor for the spawn raycast.")]
    public LayerMask groundMask = ~0;

    public Vector3 Position => GetGroundedPosition();
    public float FacingYaw => transform.eulerAngles.y;
    public Quaternion FacingRotation => Quaternion.Euler(0f, FacingYaw, 0f);

    private Vector3 GetGroundedPosition()
    {
        Vector3 origin = transform.position;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return origin;
    }

    private bool TryGetGroundedPosition(out Vector3 pos)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point;
            return true;
        }
        pos = transform.position;
        return false;
    }

    private void OnDrawGizmos()
    {
        DrawGizmos(new Color(0.2f, 0.7f, 1f, 0.35f), new Color(0.2f, 0.7f, 1f, 0.9f));
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmos(new Color(0.4f, 0.9f, 1f, 0.5f), new Color(0.6f, 1f, 1f, 1f));
    }

    private void DrawGizmos(Color fill, Color wire)
    {
        bool hit = TryGetGroundedPosition(out Vector3 landing);
        Color f = hit ? fill : new Color(1f, 0.3f, 0.3f, fill.a);
        Color w = hit ? wire : new Color(1f, 0.2f, 0.2f, 1f);

        Gizmos.color = f;
        Gizmos.DrawSphere(landing, 0.25f);
        Gizmos.color = w;
        Gizmos.DrawWireSphere(landing, 0.3f);

        Vector3 forward = Quaternion.Euler(0f, FacingYaw, 0f) * Vector3.forward;
        Vector3 tip = landing + forward * 1.25f;

        Gizmos.DrawLine(landing, tip);

        Vector3 right = Quaternion.Euler(0f, FacingYaw + 155f, 0f) * Vector3.forward * 0.35f;
        Vector3 left = Quaternion.Euler(0f, FacingYaw - 155f, 0f) * Vector3.forward * 0.35f;
        Gizmos.DrawLine(tip, tip + right);
        Gizmos.DrawLine(tip, tip + left);
    }
}
