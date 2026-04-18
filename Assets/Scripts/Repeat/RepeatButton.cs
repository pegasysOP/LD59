using System;
using UnityEngine;

public class RepeatButton : MonoBehaviour, IInteractable
{
    public enum Colour { Red, Green, Blue, Yellow }
    
    [SerializeField]
    private Colour colour;

    public event Action<Colour> OnPressed;

    public void Interact()
    {
        Debug.Log($"Button {colour} was pressed");

        OnPressed?.Invoke(colour);
        //TODO: Something?
    }

    public bool IsInteractable()
    {
        return true;
    }

    public Colour GetColour()
    {
        return colour;
    }
}
