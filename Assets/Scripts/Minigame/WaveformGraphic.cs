using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class WaveformGraphic : MaskableGraphic
{
    public float thickness = 3f;

    private readonly List<Vector2> points = new List<Vector2>();

    public void SetPoints(List<Vector2> pts)
    {
        points.Clear();
        if (pts != null) points.AddRange(pts);
        SetVerticesDirty();
    }

    public void Clear()
    {
        points.Clear();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        float half = thickness * 0.5f;
        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];
            Vector2 dir = (b - a);
            if (dir.sqrMagnitude < 0.0001f) continue;
            dir.Normalize();
            Vector2 normal = new Vector2(-dir.y, dir.x) * half;

            int baseIdx = vh.currentVertCount;

            v.position = a - normal; vh.AddVert(v);
            v.position = a + normal; vh.AddVert(v);
            v.position = b + normal; vh.AddVert(v);
            v.position = b - normal; vh.AddVert(v);

            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
        }
    }
}
