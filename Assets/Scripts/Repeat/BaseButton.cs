using System;
using System.Collections;
using UnityEngine;

public class BaseButton : MonoBehaviour
{
    private bool flashing = false;
    public void Flash(float duration)
    {
        if (flashing)
            return;
        StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        flashing = true;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;

            Color originalEmission = Color.black;
            bool hadEmission = mat.IsKeywordEnabled("_EMISSION");

            if (mat.HasProperty("_EmissionColor"))
            {
                originalEmission = mat.GetColor("_EmissionColor");
            }

            mat.EnableKeyword("_EMISSION");

            Color baseColor = mat.HasProperty("_BaseColor")
                ? mat.GetColor("_BaseColor")
                : Color.white;

            float intensity = 1.5f;
            mat.SetColor("_EmissionColor", baseColor * intensity);
        }

        yield return new WaitForSeconds(duration);

        if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }

            mat.DisableKeyword("_EMISSION");
        }

        flashing = false;
    }
}
