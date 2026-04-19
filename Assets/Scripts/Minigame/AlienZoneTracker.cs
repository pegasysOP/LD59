using System.Collections.Generic;
using UnityEngine;

public static class AlienZoneTracker
{
    private static readonly List<AlienZone> stack = new List<AlienZone>();

    public static AlienSpawnPoint CurrentSpawnPoint
    {
        get
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                AlienZone z = stack[i];
                if (z != null && z.spawnPoint != null) return z.spawnPoint;
            }
            return Fallback();
        }
    }

    public static void Push(AlienZone zone)
    {
        if (zone == null) return;
        stack.Remove(zone);
        stack.Add(zone);
    }

    public static void Pop(AlienZone zone)
    {
        if (zone == null) return;
        stack.Remove(zone);
    }

    private static AlienSpawnPoint Fallback()
    {
        IReadOnlyList<AlienSpawnPoint> all = AlienSpawnPoint.All;
        if (all.Count == 0) return null;

        Transform player = GameManager.Instance != null && GameManager.Instance.playerController != null
            ? GameManager.Instance.playerController.transform
            : null;
        if (player == null) return all[0];

        AlienSpawnPoint best = null;
        float bestSqr = float.MaxValue;
        Vector3 p = player.position;
        for (int i = 0; i < all.Count; i++)
        {
            AlienSpawnPoint sp = all[i];
            if (sp == null) continue;
            float d = (sp.Position - p).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = sp;
            }
        }
        return best;
    }
}
