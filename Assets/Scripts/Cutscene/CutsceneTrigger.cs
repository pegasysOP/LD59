using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField]
    private int cutsceneIndex;
    private bool hasPlayed = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasPlayed)
        {
            CutsceneManager.Instance.PlayCutscene(cutsceneIndex);
            hasPlayed = true;
        }
    }
}
