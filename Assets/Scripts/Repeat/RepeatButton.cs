using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RepeatButton : MonoBehaviour, IInteractable
{
    public enum Colour { Red, Green, Blue, Yellow }

    [SerializeField]
    private Colour colour;

    [SerializeField]
    private float flashTime = 0.2f;

    public event Action<Colour> OnPressed;

    public bool isInteractable = true;

    public void Interact()
    {
        Debug.Log($"Button {colour} was pressed");

        OnPressed?.Invoke(colour);

        Flash(flashTime);
    }

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

    public bool IsInteractable()
    {
        return isInteractable;
    }

    public Colour GetColour()
    {
        return colour;
    }
}