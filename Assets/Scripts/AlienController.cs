using UnityEngine;

public class AlienController : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerController == null)
            Debug.LogError("No player could be found for the alien to follow.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SlowPlayer()
    {
        Debug.LogError("Not Implemented yet.");
    }

    void StopPlayer()
    {
        Debug.LogError("Not Implemented yet.");
    }

    void TrackPlayer()
    {
        Debug.LogError("Not Implemented yet.");
    }
}
