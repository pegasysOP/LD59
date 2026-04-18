using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class HDRColorBoost : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color hdrColor = Color.white;

    private void OnEnable() { Apply(); }
    private void OnValidate() { Apply(); }

    private void Apply()
    {
        Graphic g = GetComponent<Graphic>();
        if (g != null) g.color = hdrColor;
    }
}
