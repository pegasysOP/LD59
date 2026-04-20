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

    [Serializable]
    public class DialoguePair
    {
        [TextArea] public string question;
        [TextArea] public string response;
    }

    [Header("Dialogue")]
    public CanvasGroup alienTextBox;
    public CanvasGroup playerTextBox;
    public TextMeshProUGUI alienTextLabel;
    public TextMeshProUGUI playerTextLabel;
    public DialoguePair[] dialoguePairs;
    public string[] wrongAnswerWords;
    public string gibberishChars = "!@#$%^&*?+~<>/\\|=";
    public float calculatingDelay = 1.4f;
    public string calculatingText = "Calculating response...";
    public int wrongAnswerWordCount = 6;
    public float gibberishScrambleInterval = 0.05f;
    public float playerTypeCps = 35f;
    public float titleFlashInterval = 0.5f;
    [ColorUsage(true, true)] public Color titleDefaultColor = Color.white;
    [ColorUsage(true, true)] public Color titleSuccessColor = new Color(0.07450979f, 0.10457513f, 0.9764706f, 1f);
    [ColorUsage(true, true)] public Color titleFailureColor = new Color(0.9764706f, 0.07450979f, 0.07450979f, 1f);
    public float translatingHoldDuration = 1.2f;
    public float failureDisplayDuration = 2.8f;
    public float waveformPreviewDelay = 0.6f;
    public float postResponseDelay = 3f;
    public float introDelay = 1.5f;

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
    private DialoguePair currentPair;
    private int lastPairIndex = -1;
    private readonly HashSet<int> usedPairIndices = new HashSet<int>();
    private Coroutine alienRevealCo;
    private Tweener titleFlashTween;

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
        usedPairIndices.Clear();
        lastPairIndex = -1;
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
        yield return new WaitForSecondsRealtime(introDelay);

        while (true)
        {
            state = State.AlienPlaying;
            SetTitleColor(titleDefaultColor);
            titleText.text = "Translating...";
            ClearRound();
            ResetSeeker();

            yield return PlayAlienPattern();
            yield return new WaitForSecondsRealtime(translatingHoldDuration);

            state = State.PlayerPrepare;
            titleText.text = calculatingText;
            yield return new WaitForSecondsRealtime(calculatingDelay);

            currentDuration = UnityEngine.Random.Range(minPatternDuration, maxPatternDuration);
            int clicks = UnityEngine.Random.Range(minPlayerClicks, maxPlayerClicks + 1);
            BuildPeaks(GeneratePattern(clicks, currentDuration));
            ResetSeeker();
            strayMissThisRound = false;

            yield return new WaitForSecondsRealtime(waveformPreviewDelay);

            titleText.text = "Your turn!";
            state = State.PlayerInput;
            yield return RunPlayerInput();

            state = State.RoundResolved;
            bool won = RoundWon();
            titleText.text = won ? "Response success" : "Response failure";
            SetTitleColor(won ? titleSuccessColor : titleFailureColor);
            string playerLine = won ? (currentPair != null ? currentPair.response : string.Empty) : BuildWrongAnswer();
            ShowTextBox(playerTextBox, true);
            yield return TypewriterPlayerText(playerLine, playerTypeCps);

            if (won)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                EndSession(true);
                yield break;
            }

            failCount++;
            yield return new WaitForSecondsRealtime(failureDisplayDuration);

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

            if (failCount >= maxFails)
            {
                state = State.GameOver;
                titleText.text = "Game Over";
                yield return new WaitForSecondsRealtime(0.6f);
                EndSession(false);
                yield break;
            }

            yield return new WaitForSecondsRealtime(postResponseDelay);
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

        currentPair = PickPair();
        if (currentPair != null)
        {
            ShowTextBox(alienTextBox, true);
            if (alienRevealCo != null) StopCoroutine(alienRevealCo);
            alienRevealCo = StartCoroutine(RevealAlienText(currentPair.question, duration));
        }

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
        ClearDialogue();
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

    private void ShowTextBox(CanvasGroup box, bool visible)
    {
        if (box == null) return;
        box.gameObject.SetActive(true);
        box.alpha = visible ? 1f : 0f;
    }

    private void ClearDialogue()
    {
        if (alienRevealCo != null)
        {
            StopCoroutine(alienRevealCo);
            alienRevealCo = null;
        }
        if (alienTextLabel != null) alienTextLabel.text = string.Empty;
        if (playerTextLabel != null) playerTextLabel.text = string.Empty;
        ShowTextBox(alienTextBox, false);
        ShowTextBox(playerTextBox, false);
    }

    private DialoguePair PickPair()
    {
        if (dialoguePairs == null || dialoguePairs.Length == 0) return null;
        if (usedPairIndices.Count >= dialoguePairs.Length)
            usedPairIndices.Clear();

        List<int> pool = new List<int>();
        for (int i = 0; i < dialoguePairs.Length; i++)
            if (!usedPairIndices.Contains(i) && i != lastPairIndex)
                pool.Add(i);
        if (pool.Count == 0)
            for (int i = 0; i < dialoguePairs.Length; i++)
                if (!usedPairIndices.Contains(i))
                    pool.Add(i);

        int idx = pool[UnityEngine.Random.Range(0, pool.Count)];
        usedPairIndices.Add(idx);
        lastPairIndex = idx;
        return dialoguePairs[idx];
    }

    private string MakeGibberish(int len)
    {
        if (len <= 0 || string.IsNullOrEmpty(gibberishChars)) return string.Empty;
        char[] buf = new char[len];
        for (int i = 0; i < len; i++)
            buf[i] = gibberishChars[UnityEngine.Random.Range(0, gibberishChars.Length)];
        return new string(buf);
    }

    private IEnumerator RevealAlienText(string target, float duration)
    {
        if (alienTextLabel == null || string.IsNullOrEmpty(target))
        {
            alienRevealCo = null;
            yield break;
        }

        float t = 0f;
        float scrambleTimer = 0f;
        string tail = MakeGibberish(target.Length);
        alienTextLabel.text = tail;

        while (t < duration)
        {
            t += Time.deltaTime;
            scrambleTimer += Time.deltaTime;

            int locked = Mathf.Clamp(Mathf.FloorToInt(t / Mathf.Max(0.0001f, duration) * target.Length), 0, target.Length);
            int remaining = target.Length - locked;

            if (scrambleTimer >= gibberishScrambleInterval || tail.Length != remaining)
            {
                tail = MakeGibberish(remaining);
                scrambleTimer = 0f;
            }

            alienTextLabel.text = target.Substring(0, locked) + tail;
            yield return null;
        }

        alienTextLabel.text = target;
        alienRevealCo = null;
    }

    private IEnumerator TypewriterPlayerText(string target, float cps)
    {
        if (playerTextLabel == null || string.IsNullOrEmpty(target)) yield break;
        playerTextLabel.text = string.Empty;
        float interval = cps > 0f ? 1f / cps : 0f;
        for (int i = 0; i < target.Length; i++)
        {
            playerTextLabel.text = target.Substring(0, i + 1);
            if (interval > 0f) yield return new WaitForSecondsRealtime(interval);
        }
    }

    private string BuildWrongAnswer()
    {
        if (wrongAnswerWords == null || wrongAnswerWords.Length == 0) return "...";
        int n = Mathf.Min(Mathf.Max(1, wrongAnswerWordCount), wrongAnswerWords.Length);
        List<string> pool = new List<string>(wrongAnswerWords);
        string[] picks = new string[n];
        for (int i = 0; i < n; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            picks[i] = pool[idx];
            pool.RemoveAt(idx);
        }
        string joined = string.Join(" ", picks);
        if (joined.Length > 0) joined = char.ToUpper(joined[0]) + joined.Substring(1);
        return joined + ".";
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
        StartTitleFlash();
    }

    private void HidePanel()
    {
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        if (monster3D != null) monster3D.gameObject.SetActive(false);
        RestoreHudHelpers();
        StopTitleFlash();
    }

    private void SetTitleColor(Color c)
    {
        if (titleText == null) return;
        titleText.color = c;
    }

    private void StartTitleFlash()
    {
        if (titleText == null) return;
        StopTitleFlash();
        SetTitleColor(titleDefaultColor);
        titleText.alpha = 1f;
        titleFlashTween = titleText.DOFade(0f, titleFlashInterval)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    private void StopTitleFlash()
    {
        if (titleFlashTween != null && titleFlashTween.IsActive())
            titleFlashTween.Kill();
        titleFlashTween = null;
        if (titleText != null) titleText.alpha = 1f;
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
