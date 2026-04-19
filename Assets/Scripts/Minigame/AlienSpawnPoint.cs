using System.Collections.Generic;
using UnityEngine;

public class AlienSpawnPoint : MonoBehaviour
{
    private static readonly List<AlienSpawnPoint> registry = new List<AlienSpawnPoint>();

    public static IReadOnlyList<AlienSpawnPoint> All => registry;

    [Tooltip("Max distance to raycast downward to find the floor.")]
    public float groundRaycastDistance = 10f;
    [Tooltip("Layers considered floor for the spawn raycast. Set to the ground/environment layer(s).")]
    public LayerMask groundMask = ~0;
    //[Tooltip("Vertical offset applied above the floor hit point. Raises the alien's pivot off the ground.")]
    //public float spawnHeightOffset = 1.5f;

    public Vector3 Position => GetGroundedPosition();
    public Quaternion Rotation => transform.rotation;

    private Vector3 GetGroundedPosition()
    {
        Vector3 origin = transform.position;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point;// + Vector3.up * spawnHeightOffset;
        return origin;
    }

    private bool TryGetGroundedPosition(out Vector3 pos)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point;// + Vector3.up * spawnHeightOffset;
            return true;
        }
        pos = transform.position;
        return false;
    }

    private void OnEnable()
    {
        if (!registry.Contains(this)) registry.Add(this);
    }

    private void OnDisable()
    {
        registry.Remove(this);
    }

    private void OnDrawGizmos()
    {
        DrawSpawnGizmo(new Color(0.6f, 0.2f, 0.8f, 0.35f), new Color(0.6f, 0.2f, 0.8f, 0.9f));
    }

    private void OnDrawGizmosSelected()
    {
        DrawSpawnGizmo(new Color(0.9f, 0.4f, 1f, 0.5f), new Color(1f, 0.6f, 1f, 1f));
    }

    private void DrawSpawnGizmo(Color fill, Color wire)
    {
        bool hit = TryGetGroundedPosition(out Vector3 landing);
        Color f = hit ? fill : new Color(1f, 0.3f, 0.3f, fill.a);
        Color w = hit ? wire : new Color(1f, 0.2f, 0.2f, 1f);

        Gizmos.color = f;
        Gizmos.DrawSphere(landing, 0.25f);
        Gizmos.color = w;
        Gizmos.DrawWireSphere(landing, 0.3f);
    }
}
