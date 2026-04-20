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
                StartCoroutine(EscapePodCutsceneRoutine());
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

    private IEnumerator EscapePodCutsceneRoutine()
    {
        Debug.Log("Playing Escape cutscene");

        //Immobilise Player
        GameManager.Instance?.SetLocked(true);

        //Rotate to face escape door 
        Quaternion current = controller.transform.rotation;
        Quaternion target = Quaternion.Euler(current.eulerAngles.x, 90f, current.eulerAngles.z);

        yield return StartCoroutine(RotateTo(controller.transform, target, 1f));

        yield return new WaitForSeconds(2f);

        //Have creature run towards door

        //Close door 

        //Play rocket takeoff sound 

        //Fade to black 
        yield return StartCoroutine(Fade(1f, fadeDuration));

        //Load credits scene 
        SceneUtils.LoadCreditScene();
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

    private IEnumerator RotateTo(Transform target, Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = target.rotation;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;

            // smoother feel than linear
            t = Mathf.SmoothStep(0f, 1f, t);

            target.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            time += Time.deltaTime;
            yield return null;
        }

        target.rotation = targetRotation;
    }
}
