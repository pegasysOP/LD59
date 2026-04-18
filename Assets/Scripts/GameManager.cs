using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public AudioManager audioManager;

    public PlayerController playerController;
    public CameraController cameraController;
    public HudController hudController;

    private InputAction escapeAction;

    private int totalBatteries = 3;
    private int currentBatteries = 0;

    public bool LOCKED = false;
    public bool MinigameActive = false;

    private void Awake()
    {
        Instance = this;
    }

    public void CollectBattery()
    {
        currentBatteries++;

        AudioManager.Instance?.batterySounds?.GetPowerUpFor(currentBatteries)?.Play();

        if(currentBatteries == totalBatteries)
        {
            Debug.Log("All batteries collected");

            //TODO: Implement logic for triggering some event after all batteries. 
            //e.g. powering up other rooms, opening doors etc. 
        }
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

        if (escapeAction != null && escapeAction.triggered)
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
        SetLocked(paused);
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
