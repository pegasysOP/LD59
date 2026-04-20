using System.Collections;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{

    [SerializeField]
    private PlayerController controller;

    public static CutsceneManager Instance;

    [SerializeField] private float wakeDuration = 5f;
    [SerializeField] private float powerdownDuration = 14f;
    [SerializeField] private float escapePodDuration = 6f;

    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [SerializeField] private float fadeDuration = 1f;

    public enum CutsceneType
    {
        Wake,
        Powerdown,
        EscapePod
    }

    public CutsceneType type;

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

    public void PlayCutscene(CutsceneType type)
    {
        switch (type) { 
           case CutsceneType.Wake:
                PlayWakeCutscene();
                break;
            case CutsceneType.Powerdown:
                StartCoroutine(PowerdownCutsceneRoutine());
                break;
            case CutsceneType.EscapePod:
                PlayEscapePodCutscene();
                break;
            default:
                Debug.LogError("Invalid cutscene type: " + type);
                break;
        }
    }

    private void PlayWakeCutscene()
    {
        Debug.Log("Playing wake cutscene");
        
    }

    private IEnumerator PowerdownCutsceneRoutine()
    {
        Debug.Log("Playing powerdown cutscene");

        GameManager.Instance?.SetLocked(true);

        //Fade out at start of power down cutscene
        yield return StartCoroutine(Fade(1f, fadeDuration));

        //TODO: Play powerdown visual effects here 

        yield return new WaitForSeconds(powerdownDuration);

        //Fade back in after power down is complete
        yield return StartCoroutine(Fade(0f, fadeDuration));

        GameManager.Instance?.SetLocked(false);

        Debug.Log("Powerdown cutscene finished");
    }

    private void PlayEscapePodCutscene()
    {
        Debug.Log("Playing Escape cutscene");
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, t);

            time += Time.deltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = target;
    }
}
