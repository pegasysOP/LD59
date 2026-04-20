using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Minigame : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup panelGroup;
    public Transform monster;
    public Transform monster3D;
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

    [Header("Player Hand")]
    public Animator rightHandAnimator;
    public string rightHandClickState = "Hand R Click";

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
    public float monsterDropPerFail = 0.25f;
    public float monster3DApproachDistance = 0.5f;
    public const float monster3DApproachDistanceFloor = 0.5f;
    public float monster3DWalkDuration = 0.5f;
    public Ease monster3DWalkEase = Ease.InOutSine;
    public float monster3DFallbackDistance = 3f;
    public float monster3DAimHeight = 1.7f;
    public string monster3DWalkState = "walk";
    public string monster3DIdleState = "idle 1";
    public string monster3DAttackState = "attack 1";
    public float monster3DAnimCrossfade = 0.15f;
    public Key debugStartKey = Key.M;

    public event Action<bool> OnMinigameEnded;

    /// <summary>Number of failed rounds accumulated during the active session. Resets to 0 at the start of a new session.</summary>
    public int FailCount => failCount;

    /// <summary>Maximum number of failed rounds before the session ends in Game Over.</summary>
    public int MaxFails => maxFails;

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
    private Vector3 monsterBaseScale;
    private Vector3 monsterBasePosition;
    private Vector3 monster3DSpawnPos;
    private Vector3 monster3DApproachPos;
    private Tweener monster3DWalkTween;
    private Animator monster3DAnimator;
    private bool strayMissThisRound;

    private void Start()
    {
        clickAction = InputSystem.actions?.FindAction("Click");
        monsterBaseScale = monster.localScale;
        monsterBasePosition = monster.localPosition;
        if (monster != null) monster.gameObject.SetActive(false);
        if (monster3D != null) monster3DAnimator = monster3D.GetComponentInChildren<Animator>(true);
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
        monster.localScale = monsterBaseScale;
        monster.localPosition = monsterBasePosition;
        SetupMonster3DWaypoints();
        PlaceMonster3DAtSpawn();
        ShowPanel();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.MinigameActive = true;
            GameManager.Instance.SetLocked(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (GameManager.Instance.cameraController != null)
            {
                GameManager.Instance.cameraController.lookAtTarget = monster3D;
                GameManager.Instance.cameraController.lookAtYOffset = monster3DAimHeight;
            }
        }

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
            monster.localScale *= alienGrowthPerFail;
            monster.localPosition += Vector3.down * monsterDropPerFail;
            if (failCount >= maxFails)
            {
                KillMonster3DWalk();
                FaceMonster3DAtPlayer();
                PlayMonster3DAnim(monster3DAttackState);
            }
            else
            {
                WalkMonster3DToFailStep();
            }
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

            bool clicked = clickAction != null && clickAction.WasPressedThisFrame();
            bool spaced = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (clicked || spaced)
            {
                PlayRightHandClick();
                EvaluateClick(seekerTime);
            }

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

    private void PlayRightHandClick()
    {
        if (rightHandAnimator == null || string.IsNullOrEmpty(rightHandClickState)) return;
        rightHandAnimator.Play(rightHandClickState, 0, 0f);
    }

    private void PlayClickSfx(float minPitch, float maxPitch)
    {
        AudioManager.Instance.PlaySfxWithPitchShifting(new List<AudioClip> { clickClip }, minPitch, maxPitch);
    }

    private void UpdateFailCounter()
    {
        failCounterText.text = $"Strikes: {failCount}/{maxFails}";
    }

    private void SetupMonster3DWaypoints()
    {
        Transform player = GameManager.Instance != null && GameManager.Instance.playerController != null
            ? GameManager.Instance.playerController.transform
            : null;

        AlienSpawnPoint sp = AlienZoneTracker.CurrentSpawnPoint;
        if (sp != null)
        {
            monster3DSpawnPos = sp.Position;
        }
        else if (player != null)
        {
            Debug.LogWarning("[Minigame] No AlienSpawnPoint available — falling back to player-forward ray.");
            Vector3 flat = player.forward; flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
            flat.Normalize();
            monster3DSpawnPos = player.position + flat * monster3DFallbackDistance;
        }
        else
        {
            monster3DSpawnPos = monster3D != null ? monster3D.position : Vector3.zero;
        }

        Camera cam = GameManager.Instance != null && GameManager.Instance.cameraController != null
            ? GameManager.Instance.cameraController.playerCamera
            : null;
        if (cam != null)
        {
            Vector3 camForward = cam.transform.forward; camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.0001f) camForward = Vector3.forward;
            camForward.Normalize();
            monster3DApproachPos = cam.transform.position + camForward * Mathf.Max(monster3DApproachDistanceFloor, monster3DApproachDistance);
            monster3DApproachPos.y = monster3DSpawnPos.y;
        }
        else if (player != null)
        {
            Vector3 flat = player.forward; flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
            flat.Normalize();
            monster3DApproachPos = player.position + flat * Mathf.Max(monster3DApproachDistanceFloor, monster3DApproachDistance);
            monster3DApproachPos.y = monster3DSpawnPos.y;
        }
        else
        {
            monster3DApproachPos = monster3DSpawnPos;
        }
    }

    private void PlaceMonster3DAtSpawn()
    {
        if (monster3D == null) return;
        KillMonster3DWalk();
        monster3D.position = monster3DSpawnPos;
        FaceMonster3DAtPlayer();
        PlayMonster3DAnim(monster3DIdleState);
    }

    private void PlayMonster3DAnim(string stateName)
    {
        if (monster3DAnimator == null || string.IsNullOrEmpty(stateName)) return;
        monster3DAnimator.CrossFadeInFixedTime(stateName, monster3DAnimCrossfade);
    }

    private void WalkMonster3DToFailStep()
    {
        if (monster3D == null) return;
        float t = maxFails > 0 ? Mathf.Clamp01((float)failCount / maxFails) : 1f;
        Vector3 target = Vector3.Lerp(monster3DSpawnPos, monster3DApproachPos, t);

        KillMonster3DWalk();
        PlayMonster3DAnim(monster3DWalkState);
        monster3DWalkTween = monster3D.DOMove(target, monster3DWalkDuration)
            .SetEase(monster3DWalkEase)
            .SetUpdate(true)
            .OnUpdate(FaceMonster3DAtPlayer)
            .OnComplete(() =>
            {
                FaceMonster3DAtPlayer();
                PlayMonster3DAnim(monster3DIdleState);
            });
    }

    private void FaceMonster3DAtPlayer()
    {
        if (monster3D == null) return;
        Transform player = GameManager.Instance != null && GameManager.Instance.playerController != null
            ? GameManager.Instance.playerController.transform
            : null;
        if (player == null) return;

        Vector3 toPlayer = player.position - monster3D.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;
        monster3D.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
    }

    private void KillMonster3DWalk()
    {
        if (monster3DWalkTween != null && monster3DWalkTween.IsActive())
            monster3DWalkTween.Kill();
        monster3DWalkTween = null;
    }

    private void ShowPanel()
    {
        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
        if (monster3D != null) monster3D.gameObject.SetActive(true);
        HideHudHelpers();
    }

    private void HidePanel()
    {
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        if (monster3D != null) monster3D.gameObject.SetActive(false);
        RestoreHudHelpers();
    }

    private void HideHudHelpers()
    {
        HudController hud = GameManager.Instance != null ? GameManager.Instance.hudController : null;
        if (hud == null) return;
        if (hud.centreDot != null) hud.centreDot.SetActive(false);
        if (hud.interactPrompt != null) hud.interactPrompt.SetActive(false);
    }

    private void RestoreHudHelpers()
    {
        HudController hud = GameManager.Instance != null ? GameManager.Instance.hudController : null;
        if (hud == null) return;
        if (hud.centreDot != null) hud.centreDot.SetActive(true);
    }

    private void EndSession(bool won)
    {
        KillMonster3DWalk();
        ClearRound();
        HidePanel();
        monster.localScale = monsterBaseScale;
        monster.localPosition = monsterBasePosition;

        if (GameManager.Instance != null && GameManager.Instance.cameraController != null)
            GameManager.Instance.cameraController.lookAtTarget = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.MinigameActive = false;
            GameManager.Instance.SetLocked(false);
        }

        state = State.Idle;
        OnMinigameEnded?.Invoke(won);
    }

    /*public void PlayFinalMonsterApproach()
    {
        StartCoroutine(FinalMonsterApproachRoutine());
    }

    IEnumerator FinalMonsterApproachRoutine()
    {
        if (monster3D == null) yield break;

        //ShowPanel();

        KillMonster3DWalk();

        SetupMonster3DWaypoints();
        monster3D.position = monster3DSpawnPos;

        FaceMonster3DAtPlayer();
        PlayMonster3DAnim(monster3DWalkState);

        Transform player = GameManager.Instance != null && GameManager.Instance.playerController != null
            ? GameManager.Instance.playerController.transform
            : null;

        if (player == null)
            yield break;

        float speed = 1.2f;

        while (Vector3.Distance(monster3D.position, player.position) > 0.6f)
        {
            Vector3 target = player.position;
            target.y = monster3D.position.y;

            monster3D.position = Vector3.MoveTowards(
                monster3D.position,
                target,
                speed * Time.deltaTime
            );

            
            FaceMonster3DAtPlayer();

            yield return null;
        }

        FaceMonster3DAtPlayer();
        PlayMonster3DAnim(monster3DAttackState);

        Debug.Log("Monster reached player (final cutscene)");
    }*/
}
