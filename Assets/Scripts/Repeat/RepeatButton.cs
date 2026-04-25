using System;
using UnityEngine;

public class RepeatButton : BaseButton, IInteractable
{
    public enum Colour { Red, Green, Blue, Yellow }

    [SerializeField]
    private Colour colour;

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