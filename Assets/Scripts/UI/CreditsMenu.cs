using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CreditsMenu : MonoBehaviour
{
    public Button menuButton;
    public CanvasGroup canvasGroup;
    public CanvasGroup blackOverlay;
    public float blackHoldDuration = 1f;
    public float blackFadeDuration = 1f;
    public float fadeDelay = 1.5f;
    public float fadeDuration = 1.5f;

    private void Awake()
    {
        menuButton.onClick.AddListener(OnMenuClicked);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        blackOverlay.alpha = 1f;
        blackOverlay.blocksRaycasts = true;
    }

    private void Start()
    {
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(blackHoldDuration);

        float elapsed = 0f;
        while (elapsed < blackFadeDuration)
        {
            elapsed += Time.deltaTime;
            blackOverlay.alpha = 1f - Mathf.Clamp01(elapsed / blackFadeDuration);
            yield return null;
        }
        blackOverlay.alpha = 0f;
        blackOverlay.blocksRaycasts = false;

        yield return StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void OnMenuClicked()
    {
        SceneUtils.LoadMenuScene();
    }
}
