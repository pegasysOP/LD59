using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Minigame : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup panelGroup;
    public Image alienImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI failCounterText;

    [Header("Waveform")]
    public RectTransform waveformContainer;
    public RectTransform peakParent;
    public WaveformGraphic waveform;
    public Image tickPrefab;
    public Image crossPrefab;
    public RectTransform seeker;
    public float peakHeight = 80f;
    public float peakHalfWidthSec = 0.08f;

    [Header("Audio")]
    public AudioClip clickClip;
    public AudioManager audioManager;

    [Header("Tuning")]
    public float hitTolerance = 0.18f;
    public int minPlayerClicks = 3;
    public int maxPlayerClicks = 5;
    public float minPatternDuration = 1.5f;
    public float maxPatternDuration = 2f;
    public int minAlienClicks = 4;
    public int maxAlienClicks = 8;
    public float minAlienDuration = 3f;
    public float maxAlienDuration = 4f;
    public int maxFails = 3;
    public float alienGrowthPerFail = 1.25f;
    public Key debugStartKey = Key.M;

    public event Action<bool> OnMinigameEnded;

    private enum State { Idle, AlienPlaying, PlayerPrepare, PlayerInput, RoundResolved, GameOver }

    private class Peak
    {
        public float time;
        public bool hit;
        public bool autoMarked;
    }

    private State state = State.Idle;
    private InputAction clickAction;
    private readonly List<Peak> peaks = new List<Peak>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private float currentDuration;
    private int failCount;
    private Vector3 alienBaseScale;
    private bool strayMissThisRound;

    private void Start()
    {
        clickAction = InputSystem.actions?.FindAction("Click");
        alienBaseScale = alienImage.rectTransform.localScale;
        HidePanel();
        UpdateFailCounter();
    }

    private void Update()
    {
        if (state == State.Idle
            && Keyboard.current != null
            && Keyboard.current[debugStartKey].wasPressedThisFrame)
            StartMinigame();
    }

    public void StartMinigame()
    {
        if (state != State.Idle) return;

        failCount = 0;
        UpdateFailCounter();
        alienImage.rectTransform.localScale = alienBaseScale;
        ShowPanel();

        if (GameManager.Instance != null)
            GameManager.Instance.SetLocked(true);

        StartCoroutine(RunSession());
    }

    private IEnumerator RunSession()
    {
        while (true)
        {
            state = State.AlienPlaying;
            titleText.text = "Listen...";
            ClearRound();
            ResetSeeker();

            yield return PlayAlienPattern();

            state = State.PlayerPrepare;
            titleText.text = "Your turn!";
            yield return new WaitForSecondsRealtime(0.6f);

            currentDuration = UnityEngine.Random.Range(minPatternDuration, maxPatternDuration);
            int clicks = UnityEngine.Random.Range(minPlayerClicks, maxPlayerClicks + 1);
            BuildPeaks(GeneratePattern(clicks, currentDuration));
            ResetSeeker();
            strayMissThisRound = false;

            state = State.PlayerInput;
            yield return RunPlayerInput();

            state = State.RoundResolved;
            if (RoundWon())
            {
                yield return new WaitForSecondsRealtime(1f);
                EndSession(true);
                yield break;
            }

            failCount++;
            UpdateFailCounter();
            alienImage.rectTransform.localScale *= alienGrowthPerFail;
            yield return new WaitForSecondsRealtime(0.8f);

            if (failCount >= maxFails)
            {
                state = State.GameOver;
                titleText.text = "Game Over";
                yield return new WaitForSecondsRealtime(0.6f);
                EndSession(false);
                yield break;
            }
        }
    }

    private IEnumerator RunPlayerInput()
    {
        float seekerTime = 0f;
        float width = waveformContainer.rect.width;
        float endTime = currentDuration + hitTolerance;

        while (seekerTime < endTime)
        {
            seekerTime += Time.deltaTime;
            seeker.anchoredPosition = new Vector2(TimeToX(seekerTime, width), 0f);

            foreach (Peak p in peaks)
            {
                if (!p.hit && !p.autoMarked && seekerTime > p.time + hitTolerance)
                {
                    p.autoMarked = true;
                    SpawnMarker(crossPrefab, p.time);
                }
            }

            if (clickAction != null && clickAction.WasPressedThisFrame())
                EvaluateClick(seekerTime);

            yield return null;
        }
    }

    private bool RoundWon()
    {
        if (strayMissThisRound) return false;
        foreach (Peak p in peaks)
            if (!p.hit) return false;
        return true;
    }

    private IEnumerator PlayAlienPattern()
    {
        int count = UnityEngine.Random.Range(minAlienClicks, maxAlienClicks + 1);
        float duration = UnityEngine.Random.Range(minAlienDuration, maxAlienDuration);
        List<float> times = GeneratePattern(count, duration);

        float t = 0f;
        int idx = 0;
        while (t < duration && idx < times.Count)
        {
            if (t >= times[idx])
            {
                PlayClickSfx(0.9f, 1.1f);
                idx++;
            }
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
    }

    private List<float> GeneratePattern(int count, float duration)
    {
        float minT = 0.2f;
        float maxT = Mathf.Max(minT + 0.1f, duration - 0.2f);
        float span = maxT - minT;
        float minGap = Mathf.Min(0.15f, span / (count + 1));
        float flex = span - minGap * (count - 1);

        float[] weights = new float[count + 1];
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = UnityEngine.Random.value + 0.1f;
            total += weights[i];
        }

        List<float> times = new List<float>(count);
        float cursor = minT + weights[0] / total * flex;
        times.Add(cursor);
        for (int i = 1; i < count; i++)
        {
            cursor += minGap + weights[i] / total * flex;
            times.Add(cursor);
        }
        return times;
    }

    private void BuildPeaks(List<float> times)
    {
        float width = waveformContainer.rect.width;
        float halfSpikePx = (peakHalfWidthSec / currentDuration) * width;

        List<Vector2> pts = new List<Vector2>();
        pts.Add(new Vector2(0f, 0f));

        foreach (float time in times)
        {
            float x = TimeToX(time, width);
            pts.Add(new Vector2(x - halfSpikePx, 0f));
            pts.Add(new Vector2(x, peakHeight));
            pts.Add(new Vector2(x + halfSpikePx, 0f));

            peaks.Add(new Peak { time = time });
        }

        pts.Add(new Vector2(width, 0f));
        waveform.SetPoints(pts);
    }

    private void SpawnMarker(Image prefab, float atTime)
    {
        Image img = Instantiate(prefab, peakParent);
        img.gameObject.SetActive(true);
        RectTransform rt = img.rectTransform;
        rt.anchoredPosition = new Vector2(TimeToX(atTime, waveformContainer.rect.width), rt.anchoredPosition.y);
        spawned.Add(img.gameObject);
    }

    private float TimeToX(float time, float width)
    {
        return Mathf.Lerp(0f, width, Mathf.Clamp01(time / Mathf.Max(0.0001f, currentDuration)));
    }

    private void ClearRound()
    {
        foreach (GameObject go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
        peaks.Clear();
        waveform.Clear();
    }

    private void ResetSeeker()
    {
        seeker.anchoredPosition = Vector2.zero;
    }

    private void EvaluateClick(float seekerTime)
    {
        int bestIdx = -1;
        float bestDelta = float.MaxValue;
        for (int i = 0; i < peaks.Count; i++)
        {
            if (peaks[i].hit) continue;
            float delta = Mathf.Abs(peaks[i].time - seekerTime);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestIdx = i;
            }
        }

        if (bestIdx >= 0 && bestDelta <= hitTolerance)
        {
            peaks[bestIdx].hit = true;
            SpawnMarker(tickPrefab, peaks[bestIdx].time);
            PlayClickSfx(0.95f, 1.05f);
        }
        else
        {
            strayMissThisRound = true;
            SpawnMarker(crossPrefab, seekerTime);
        }
    }

    private void PlayClickSfx(float minPitch, float maxPitch)
    {
        audioManager.PlaySfxWithPitchShifting(new List<AudioClip> { clickClip }, minPitch, maxPitch);
    }

    private void UpdateFailCounter()
    {
        failCounterText.text = $"Strikes: {failCount}/{maxFails}";
    }

    private void ShowPanel()
    {
        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
    }

    private void EndSession(bool won)
    {
        ClearRound();
        HidePanel();
        alienImage.rectTransform.localScale = alienBaseScale;

        if (GameManager.Instance != null)
            GameManager.Instance.SetLocked(false);

        state = State.Idle;
        OnMinigameEnded?.Invoke(won);
    }
}
