using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public AudioManager audioManager;

    public PlayerController playerController;
    public CameraController cameraController;
    public HudController hudController;

    [SerializeField]
    public Minigame minigame;

    [SerializeField]
    private float minigameStartDelay = 7f;

    private InputAction escapeAction;

    private int totalBatteries = 3;
    private int currentBatteries = 0;

    public bool LOCKED = false;
    public bool MinigameActive = false;

    private void Awake()
    {
        Instance = this;
    }

    public void CollectBattery(Vector3 slotPosition)
    {
        currentBatteries++;

        //TODO: Play ambient sounds during batteries

        // Power-up stinger is a diegetic event at the slot - route through the positional path
        // so it pans/attenuates relative to the player and picks up the room's reverb.
        AudioManager.Instance?.batterySounds?.GetPowerUpFor(currentBatteries)?.PlayAt(slotPosition);

        if(currentBatteries == totalBatteries)
        {
            Debug.Log("All batteries collected");

            StateTracker.Instance?.CompleteTask(TaskType.Batteries);

            //TODO: Play a sound while waiting for the minigame to start
            StartCoroutine(StartMinigameAfterDelay(minigameStartDelay));

            //TODO: Implement logic for triggering some event after all batteries.
            //e.g. powering up other rooms, opening doors etc.
        }
    }

    public IEnumerator StartMinigameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        minigame.StartMinigame();
    }

    private void Start()
    {
        audioManager.Init();

        if (hudController == null)
            return;

        escapeAction = InputSystem.actions.FindAction("Escape");

        UpdateUI();

        SetLocked(false);
    }

    private void UpdateUI()
    {
        //TODO: Implement UI 
    }

    private void Update()
    {
        if (hudController == null)
            return;

#if !UNITY_WEBGL || UNITY_EDITOR
        if (escapeAction != null && escapeAction.triggered)
        {
            TogglePauseMenu();
            return;
        }
#endif

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        SetPaused(!hudController.pauseMenu.IsOpen);
    }

    public void SetPaused(bool paused)
    {
        // When a minigame is running, player movement stays locked regardless of the
        // pause menu state. Closing the menu mid-minigame should only re-hide the
        // cursor, not free the camera/movement.
        if (paused)
        {
            SetLocked(true);
        }
        else if (MinigameActive)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            SetLocked(false);
        }

        hudController.pauseMenu.SetOpen(paused);
    }

    public void DestroySelf()
    {
        Time.timeScale = 1;

        Destroy(gameObject);
    }

    public void SetLocked(bool locked)
    {
        LOCKED = locked;
        Cursor.visible = locked;
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
