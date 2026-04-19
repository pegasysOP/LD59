using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AlienZone : MonoBehaviour
{
    public AlienSpawnPoint spawnPoint;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        AlienZoneTracker.Push(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        AlienZoneTracker.Pop(this);
    }

    private void OnDisable()
    {
        AlienZoneTracker.Pop(this);
    }

    private void OnDrawGizmos()
    {
        DrawZoneGizmo(new Color(0.2f, 0.8f, 1f, 0.15f), new Color(0.2f, 0.8f, 1f, 0.8f));
    }

    private void OnDrawGizmosSelected()
    {
        DrawZoneGizmo(new Color(0.2f, 0.8f, 1f, 0.35f), new Color(0.4f, 1f, 1f, 1f));
    }

    private void DrawZoneGizmo(Color fill, Color wire)
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;

        if (c is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = fill;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = wire;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (c is SphereCollider sph)
        {
            Vector3 worldCenter = transform.TransformPoint(sph.center);
            float radius = sph.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Gizmos.color = fill;
            Gizmos.DrawSphere(worldCenter, radius);
            Gizmos.color = wire;
            Gizmos.DrawWireSphere(worldCenter, radius);
        }
        else
        {
            Bounds b = c.bounds;
            Gizmos.color = fill;
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = wire;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        if (spawnPoint != null)
        {
            Gizmos.color = new Color(0.6f, 0.2f, 0.8f, 0.9f);
            Gizmos.DrawLine(c.bounds.center, spawnPoint.Position);
        }
    }
}
