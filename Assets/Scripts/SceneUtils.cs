using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneUtils
{
    public const string GAME_SCENE = "Game";
    public const string MENU_SCENE = "Menu";
    public const string CREDIT_SCENE = "Credits";

    public static void LoadGameScene()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(GAME_SCENE);
    }

    public static void LoadMenuScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameManager.Instance?.DestroySelf();

        SceneManager.LoadScene(MENU_SCENE);
    }

    public static void LoadCreditScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameManager.Instance?.DestroySelf();

        SceneManager.LoadScene(CREDIT_SCENE);
    }

    public static void QuitApplication()
    {
        Application.Quit();
    }
}
