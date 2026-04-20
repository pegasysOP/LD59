using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI <see cref="Slider"/> GameObject to get down / release /
/// drag-loop feedback routed through <see cref="AudioManager"/>. The loop
/// only plays while the handle is <i>actually moving</i> — on pointer down
/// the loop primes but stays silent, and it auto-silences after a short
/// idle window (<see cref="moveLoopIdleCooldown"/>) so a plain click on
/// the track doesn't leave the whine ringing out.
/// <para>The loop uses a dedicated child <see cref="AudioSource"/> rather
/// than <see cref="AudioSource.PlayOneShot"/> because we need to start /
/// stop it asynchronously with the drag interaction; one-shots can't be
/// cancelled once fired.</para>
/// </summary>
[RequireComponent(typeof(Slider))]
public class UiSliderSounds : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("How long (seconds, unscaled) after the last value change to " +
             "keep the move-loop audible before muting it. Small values feel " +
             "snappy; too-small values chop the loop during continuous drags.")]
    [SerializeField] private float moveLoopIdleCooldown = 0.12f;

    [Tooltip("Per-instance gain applied on top of the UI bus volume. Use to " +
             "tame the move loop if it's too prominent relative to the " +
             "down / release one-shots.")]
    [SerializeField, Range(0f, 1f)] private float moveLoopVolume = 1f;

    private Slider _slider;
    private AudioSource _loopSource;
    private bool _pointerHeld;
    private float _lastValueChangeUnscaledTime;
    private float _lastKnownValue;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _lastKnownValue = _slider != null ? _slider.value : 0f;
    }

    private void OnEnable()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(OnValueChanged);
            _lastKnownValue = _slider.value;
        }
    }

    private void OnDisable()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnValueChanged);
        }
        StopLoop();
        _pointerHeld = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_slider != null && !_slider.IsInteractable()) return;

        _pointerHeld = true;

        // Snapshot the value at pointer-down so we can distinguish "click on
        // track moved the handle" (which will also fire onValueChanged right
        // after this) from a dead click. Both paths want the down click.
        if (_slider != null) _lastKnownValue = _slider.value;

        PlayOneShot(GetClip(c => c.sliderDownClip));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pointerHeld) return;
        _pointerHeld = false;

        StopLoop();
        PlayOneShot(GetClip(c => c.sliderReleaseClip));
    }

    private void OnValueChanged(float newValue)
    {
        // Tiny diffs (e.g. a rebind / programmatic set) shouldn't trigger the
        // move loop; we only want audible movement while the user is actively
        // dragging.
        if (!_pointerHeld) { _lastKnownValue = newValue; return; }
        if (Mathf.Approximately(newValue, _lastKnownValue)) return;

        _lastKnownValue = newValue;
        _lastValueChangeUnscaledTime = Time.unscaledTime;
        EnsureLoopPlaying();
    }

    private void Update()
    {
        if (_loopSource == null) return;

        if (_loopSource.isPlaying)
        {
            // Track live UI-bus volume so the loop reacts to the user dragging
            // the volume slider itself (meta!) without having to re-resolve it
            // from AudioManager on every value change.
            _loopSource.volume = GetUiBusVolume() * Mathf.Clamp01(moveLoopVolume);

            bool idleTooLong = Time.unscaledTime - _lastValueChangeUnscaledTime > moveLoopIdleCooldown;
            if (!_pointerHeld || idleTooLong)
            {
                StopLoop();
            }
        }
    }

    private void EnsureLoopPlaying()
    {
        AudioClip clip = GetClip(c => c.sliderMoveLoopClip);
        if (clip == null || AudioManager.Instance == null) return;

        if (_loopSource == null)
        {
            // Child GameObject keeps the AudioSource cleanly scoped so pooled /
            // reused slider instances don't accumulate sources on the root.
            GameObject go = new GameObject("SliderMoveLoopSource");
            go.transform.SetParent(transform, false);
            _loopSource = go.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = 0f;
            AudioSource uiBus = AudioManager.Instance.uiSfxSource;
            if (uiBus != null)
            {
                _loopSource.outputAudioMixerGroup = uiBus.outputAudioMixerGroup;
            }
        }

        if (_loopSource.clip != clip)
        {
            _loopSource.clip = clip;
        }

        _loopSource.volume = GetUiBusVolume() * Mathf.Clamp01(moveLoopVolume);

        if (!_loopSource.isPlaying)
        {
            _loopSource.Play();
        }
    }

    private void StopLoop()
    {
        if (_loopSource != null && _loopSource.isPlaying)
        {
            _loopSource.Stop();
        }
    }

    private static void PlayOneShot(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null) return;
        AudioManager.Instance.PlayUiSfx(clip);
    }

    private delegate AudioClip ClipSelector(AudioManager am);

    private static AudioClip GetClip(ClipSelector selector)
    {
        AudioManager am = AudioManager.Instance;
        return am != null ? selector(am) : null;
    }

    private static float GetUiBusVolume()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null || am.uiSfxSource == null) return 1f;
        return am.uiSfxSource.volume;
    }
}
