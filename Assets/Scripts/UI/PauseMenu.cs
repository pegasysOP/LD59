using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public Button resumeButton;
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    public Button quitButton;

    public bool IsOpen => gameObject.activeSelf;

    private void OnEnable()
    {
        sensitivitySlider.SetValueWithoutNotify(SettingsUtils.GetSensitivity());
        volumeSlider.SetValueWithoutNotify(SettingsUtils.GetMasterVolume());

        resumeButton.onClick.AddListener(OnResumeButtonClick);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityValueChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeValueChanged);
        quitButton.onClick.AddListener(OnQuitButtonClick);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUiSfx(AudioManager.Instance.pauseMenuOpenClip);
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveListener(OnResumeButtonClick);
        sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityValueChanged);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeValueChanged);
        quitButton.onClick.RemoveListener(OnQuitButtonClick);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUiSfx(AudioManager.Instance.pauseMenuClosedClip);
    }

    public void SetOpen(bool open)
    {
        gameObject.SetActive(open);
    }

    private void OnSensitivityValueChanged(float newValue)
    {
        SettingsUtils.SetSensitivity(newValue);
        GameManager.Instance.cameraController.UpdateSensitivity(newValue);
    }

    private void OnVolumeValueChanged(float newValue)
    {
        SettingsUtils.SetMasterVolume(newValue);
        AudioManager.Instance.UpdateVolume(newValue);
    }

    private void OnResumeButtonClick()
    {
        GameManager.Instance.SetPaused(false);
    }

    private void OnQuitButtonClick()
    {
        SceneUtils.LoadMenuScene();
    }
}
