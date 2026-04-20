using UnityEngine;

public class CutsceneManager : MonoBehaviour
{

    [SerializeField]
    private PlayerController controller;

    public static CutsceneManager Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayCutscene(int index)
    {
        switch (index) { 
           case 0:
                PlayWakeCutscene();
                break;
            case 1:
                PlayPowerdownCutscene();
                break;
            case 2:
                PlayEscapePodCutscene();
                break;
            default:
                Debug.LogError("Invalid index: " + index);
                break;
        }
    }

    private void PlayWakeCutscene()
    {
        Debug.Log("Playing wake cutscene");
        
    }

    private void PlayPowerdownCutscene()
    {
        Debug.Log("Playing powerdown cutscene");
        //GameManager.Instance.LOCKED = true;
    }

    private void PlayEscapePodCutscene()
    {
        Debug.Log("Playing Escape cutscene");
    }
}
