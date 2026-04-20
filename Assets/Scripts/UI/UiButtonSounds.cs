using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to any UI object that has a <see cref="Selectable"/> (typically a
/// <see cref="Button"/>) to get hover + click feedback routed through
/// <see cref="AudioManager.PlayUiSfx"/>. Coexists with the stock Button
/// because Unity's EventSystem dispatches pointer events to every handler
/// component on the GameObject, so this script doesn't replace or suppress
/// the built-in click behaviour.
/// <para>Skips playback when the <see cref="Selectable"/> is not
/// interactable, so disabled buttons don't chirp on hover.</para>
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UiButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        PlayUi(AudioManager.Instance != null ? AudioManager.Instance.buttonHoverClip : null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        PlayUi(AudioManager.Instance != null ? AudioManager.Instance.buttonPressClip : null);
    }

    private bool IsInteractable()
    {
        return _selectable == null || _selectable.IsInteractable();
    }

    private static void PlayUi(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null) return;
        AudioManager.Instance.PlayUiSfx(clip);
    }
}
