using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PulseText : MonoBehaviour
{
    public float pulseInterval = 0.5f;

    private TextMeshProUGUI text;
    private Tweener pulseTween;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        text.alpha = 1f;
        pulseTween = text.DOFade(0f, pulseInterval)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        if (pulseTween != null && pulseTween.IsActive())
            pulseTween.Kill();
        pulseTween = null;
        if (text != null) text.alpha = 1f;
    }
}
