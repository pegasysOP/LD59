using UnityEngine;

public class Battery : MonoBehaviour, IInteractable
{
    PlayerController playerController;

    public void Interact()
    {
        //TODO: Set player to parent of battery so it moves with the player. 
        Debug.Log("Interacted with battery.");
        Debug.LogWarning("Battery interact not yet implemented");
    }

    public bool IsInteractable()
    {
        //TODO: If the player is close enough to the battery, return true. Otherwise, return false.
        return true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
