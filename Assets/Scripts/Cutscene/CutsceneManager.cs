using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip("Seconds to wait after a minigame loss before starting the fade-to-black. " +
             "Gives the monster time to play its attack animation.")]
    [SerializeField] private float lossPreFadeDelay = 1f;

    [Header("Loss UI")]
    [SerializeField] private CanvasGroup tryAgainCanvasGroup;
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private float tryAgainAppearDelay = 1f;
    [SerializeField] private float tryAgainFadeDuration = 1f;

    [SerializeField] private float postIntroLightingIntensity = 0.15f;

    [SerializeField] private GameObject[] decalsToSpawnAfterIntro;

    [Header("End Sequence")]
    [Tooltip("Where the player is gently walked to before the alien charge. Its Y yaw is used for the facing rotation; position raycasts down to the floor.")]
    [SerializeField] private EndStandPoint endStandPoint;
    [Tooltip("Scene-placed alien marker that spawns and runs toward the player.")]
    [SerializeField] private EndCutsceneAlien endAlien;
    [Tooltip("Seconds the alien takes to cover its approach distance.")]
    [SerializeField] private float alienRunDuration = 2.5f;
    [Tooltip("Delay between the alien reaching its approach point and the camera shake firing. Keep small; door timing is controlled separately.")]
    [SerializeField] private float postAlienHitDoorDelay = 0.15f;
    [Tooltip("Seconds before the alien finishes its run to start closing the door. Tune so the door finishes closing just before the alien arrives.")]
    [SerializeField] private float doorCloseLeadTime = 0.7f;
    [Tooltip("How long the door takes to slam shut during the end cutscene. Overrides the normal open/close duration for this sequence.")]
    [SerializeField] private float endDoorCloseDuration = 0.6f;
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField] private float shakeMagnitude = 0.08f;
    [Tooltip("Delay between the camera shake finishing and the fade-to-black starting.")]
    [SerializeField] private float postShakeDelay = 0.8f;
    [Tooltip("Seconds to smoothly rotate the player to face down the hallway.")]
    [SerializeField] private float endRotateDuration = 1f;

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

        if (StateTracker.Instance != null)
            StateTracker.Instance.OnEndStateChanged += HandleEndStateChanged;

        if (tryAgainCanvasGroup != null)
        {
            tryAgainCanvasGroup.alpha = 0f;
            tryAgainCanvasGroup.interactable = false;
            tryAgainCanvasGroup.blocksRaycasts = false;
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveListener(OnTryAgainClicked);
            tryAgainButton.onClick.AddListener(OnTryAgainClicked);
        }
    }

    private void OnDestroy()
    {
        if (StateTracker.Instance != null)
            StateTracker.Instance.OnEndStateChanged -= HandleEndStateChanged;

        if (tryAgainButton != null)
            tryAgainButton.onClick.RemoveListener(OnTryAgainClicked);

        if (Instance == this)
            Instance = null;
    }

    private bool lossRoutineStarted;

    private void HandleEndStateChanged(EndState state)
    {
        if (state != EndState.Lost) return;
        if (lossRoutineStarted) return;
        lossRoutineStarted = true;
        StartCoroutine(LossRoutine());
    }

    private IEnumerator LossRoutine()
    {
        GameManager.Instance?.SetLocked(true);

        IntensityManager.Instance.increasePerSecond = 0f;

        yield return new WaitForSeconds(lossPreFadeDelay);

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 1f;

        // Hold on black. Player stays locked (movement frozen) with cursor unlocked
        // via GameManager.SetLocked, so no further action needed.

        if (tryAgainCanvasGroup != null)
        {
            yield return new WaitForSeconds(tryAgainAppearDelay);

            yield return StartCoroutine(FadeCanvasGroup(tryAgainCanvasGroup, 0f, 1f, tryAgainFadeDuration));
            tryAgainCanvasGroup.interactable = true;
            tryAgainCanvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            group.alpha = Mathf.Lerp(from, to, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        group.alpha = to;
    }

    private void OnTryAgainClicked()
    {
        SceneUtils.LoadGameScene();
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

        StateTracker.Instance?.TriggerVictory();
        IntensityManager.Instance.increasePerSecond = 0f;

        // Kill minigame if somehow still running — safety net so the HUD doesn't
        // linger and StateTracker doesn't get a Lost flip from a racing EndSession.
        if (minigame != null)
            minigame.ForceStopForEndSequence();

        // Immobilise player (blocks input) + hide cursor for cinematic lock-in.
        GameManager.Instance?.SetLocked(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Hide the centre-dot reticle so it doesn't sit over the cinematic.
        HudController hud = GameManager.Instance != null ? GameManager.Instance.hudController : null;
        if (hud != null && hud.centreDot != null)
            hud.centreDot.SetActive(false);

        // Spawn the alien immediately so it's visible at the far end of the hallway
        // by the time the player has walked to the stand point and rotated.
        if (endAlien != null)
            endAlien.Activate();

        // Walk player gently to the designated stand point at their normal moveSpeed.
        controller.SetInCutscene(true);
        if (endStandPoint != null)
            yield return StartCoroutine(controller.WalkTo(endStandPoint.Position));

        // Rotate to face down the hallway (yaw taken from stand point).
        Quaternion current = controller.transform.rotation;
        float targetYaw = endStandPoint != null ? endStandPoint.FacingYaw : 90f;
        Quaternion target = Quaternion.Euler(current.eulerAngles.x, targetYaw, current.eulerAngles.z);
        yield return StartCoroutine(RotateTo(controller.transform, target, endRotateDuration));

        // Alien (already spawned at trigger time) starts charging. Kick off without awaiting
        // so we can schedule the door close mid-run — the door should finish slamming just
        // before the alien arrives and the shake fires.
        Coroutine alienRun = null;
        if (endAlien != null)
            alienRun = StartCoroutine(endAlien.RunTowardPlayer(controller.transform, alienRunDuration));

        if (alienRun != null)
        {
            float lead = Mathf.Clamp(doorCloseLeadTime, 0f, alienRunDuration);
            float waitBeforeDoor = Mathf.Max(0f, alienRunDuration - lead);
            if (waitBeforeDoor > 0f)
                yield return new WaitForSeconds(waitBeforeDoor);

            if (escapePodDoor != null)
                escapePodDoor.OpenDoorEndCutscene(endDoorCloseDuration);

            yield return alienRun;
        }
        else if (escapePodDoor != null)
        {
            escapePodDoor.OpenDoorEndCutscene(endDoorCloseDuration);
        }

        // Beat between alien arrival and the hit-effect shake.
        if (postAlienHitDoorDelay > 0f)
            yield return new WaitForSeconds(postAlienHitDoorDelay);

        // Quick shake as if the alien slammed into the door.
        CameraController cam = GameManager.Instance != null ? GameManager.Instance.cameraController : null;
        if (cam != null)
            yield return StartCoroutine(cam.Shake(shakeDuration, shakeMagnitude));

        if (postShakeDelay > 0f)
            yield return new WaitForSeconds(postShakeDelay);

        // Fade to black, load credits (unchanged).
        yield return StartCoroutine(Fade(1f, fadeDuration));
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
