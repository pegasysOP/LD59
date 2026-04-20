using System.Collections;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{

    [SerializeField]
    private PlayerController controller;

    [SerializeField]
    private GameObject torch;

    [SerializeField]
    private EscapePodDoor escapePodDoor;

    [SerializeField]
    private Minigame minigame;

    public static CutsceneManager Instance;

    /// <summary>
    /// True once the opening power-down (intro) cutscene has finished playing.
    /// Ambient/atmospheric systems that should stay silent during the scripted intro
    /// (e.g. <see cref="MachineryAmbientPlayer"/>) gate their triggers on this flag.
    /// </summary>
    public static bool IntroComplete { get; private set; }

    [Header("Timings")]
    [SerializeField] private float wakeDuration = 5f;
    [SerializeField] private float powerdownDuration = 14f;
    [SerializeField] private float escapePodDuration = 6f;
    [SerializeField] private float lockedDuration = 5.5f;

    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [SerializeField] private float fadeDuration = 1f;

    [SerializeField] private float postIntroLightingIntensity = 0.15f;

    [SerializeField] private GameObject[] decalsToSpawnAfterIntro;

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

        // Reset on fresh scene load in case domain reload was skipped and the flag
        // from a previous play-mode session is still hanging around.
        IntroComplete = false;
    }

    private void Start()
    {
        foreach (var decal in decalsToSpawnAfterIntro)
        {
            decal.SetActive(false);
        }
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

        //yield return new WaitForSeconds(1f);

        //GameManager.Instance?.SetLocked(true);

        yield return new WaitForSeconds(5.5f);


        //Fade out at start of power down cutscene
        yield return StartCoroutine(Fade(1.0f, fadeDuration + 0.5f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = postIntroLightingIntensity;

        foreach (var decal in decalsToSpawnAfterIntro)
        {
            decal.SetActive(true);
        }

        torch.SetActive(true);

        //TODO: Play powerdown visual effects here 

        //Fade back in after power down is complete
        yield return StartCoroutine(Fade(0f, fadeDuration));

        GameManager.Instance?.SetLocked(false);

        IntroComplete = true;

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

        //TODO: We also want to move the camera view towards the 0 in the vertical incase they are looking at the floor or ceiling 

        //minigame.PlayFinalMonsterApproach();

        yield return new WaitForSeconds(2f);

        //Have creature run towards door

        //Close door 
        escapePodDoor.OpenDoorEndCutscene();
    
        yield return new WaitForSeconds(1f);

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
