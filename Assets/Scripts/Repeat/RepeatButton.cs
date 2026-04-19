using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RepeatButton : BaseButton, IInteractable
{
    public enum Colour { Red, Green, Blue, Yellow }

    [SerializeField]
    private Colour colour;

    [SerializeField]
    private float flashTime = 0.2f;

    public event Action<Colour> OnPressed;

    public bool isInteractable = false;

    public void Interact()
    {
        Debug.Log($"Button {colour} was pressed");

        OnPressed?.Invoke(colour);
        if(isInteractable)
            Flash(flashTime);
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