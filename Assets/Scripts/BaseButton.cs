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

        Color original = Color.white;
        bool hasOriginal = false;

        if (meshRenderer != null)
        {
            original = meshRenderer.material.color;
            hasOriginal = true;
            Color bright = new Color(Mathf.Min(original.r * 1.8f, 1f), Mathf.Min(original.g * 1.8f, 1f), Mathf.Min(original.b * 1.8f, 1f), original.a);
            meshRenderer.material.color = bright;
        }

        yield return new WaitForSeconds(duration);

        if (hasOriginal)
        {
            if (meshRenderer != null) meshRenderer.material.color = original;
        }

        flashing = false;
    }
}
