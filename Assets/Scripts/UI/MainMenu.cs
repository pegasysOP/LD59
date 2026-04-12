using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

#if UNITY_WEBGL
        quitButton.gameObject.SetActive(false);
#endif
    }

    private void OnStartClicked()
    {
        SceneUtils.LoadGameScene();
    }

    private void OnQuitClicked()
    {
        SceneUtils.QuitApplication();
    }
}
