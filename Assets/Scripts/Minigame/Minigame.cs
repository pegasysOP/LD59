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
    [SerializeField] private TorchController torchController;

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
    [Tooltip("ScriptableObject bundling the monster appearance stinger, monster syllables (alien pattern beats), " +
             "ambient grumble pool, and player snap feedback. Leave any pool empty to silence that channel.")]
    public MonsterMinigameSounds sounds;

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
    [Tooltip("Seeker speed multiplier indexed by failCount at the start of the round. " +
             "Round 0 (no fails yet) uses index 0, after 1 fail uses index 1, etc. " +
             "Clamped to the last entry once failCount exceeds the array length.")]
    public float[] seekerSpeedByFailCount = { 1f, 0.85f, 0.70f };
    public float alienGrowthPerFail = 1.25f;
    public float monsterDropPerFail = 0.25f;
    [Tooltip("Distance in front of the player the monster reaches on the final fail step.")]
    public float monster3DApproachDistance = 1f;
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
    [Tooltip("Editor-only cheat key that auditions the full death sequence in place: fires the death " +
             "stinger, fades the BGM via MusicManager, and triggers the death-sounds array (each entry's " +
             "Delay staggers the layers). Fires regardless of minigame state so you can chain rapid " +
             "listens without having to lose the minigame. Debug builds / shipped builds ignore it.")]
    public Key debugDeathSequenceKey = Key.K;

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
    private InputAction interactAction;
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
    private Coroutine idleVocalsRoutine;
    private DialoguePair currentPair;
    private readonly List<int> shuffledPairOrder = new List<int>();
    private int shuffledPairCursor;
    private Coroutine alienRevealCo;
    private Tweener titleFlashTween;

    private void Start()
    {
        clickAction = InputSystem.actions?.FindAction("Click");
        interactAction = InputSystem.actions?.FindAction("Interact");
        monsterBaseScale = monster.localScale;
        monsterBasePosition = monster.localPosition;
        if (monster != null) monster.gameObject.SetActive(false);
        if (monster3D != null) monster3DAnimator = monster3D.GetComponentInChildren<Animator>(true);
        BuildShuffledPairOrder();
        HidePanel();
        UpdateFailCounter();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (state == State.Idle
            && Keyboard.current != null
            && Keyboard.current[debugStartKey].wasPressedThisFrame)
            StartMinigame();

        if (Keyboard.current != null
            && Keyboard.current[debugDeathSequenceKey].wasPressedThisFrame)
            DebugAuditionDeathSequence();
#endif
    }

#if UNITY_EDITOR
    // Dev audition for the death sequence: fires the full beat (stinger + BGM fade + death
    // sounds array), then immediately clears the MusicManager suspension flag so GameMusicGuy
    // can push music back in and subsequent presses re-audition cleanly. The actual death
    // path keeps music suspended (handled by PlayMonsterDeathSequence) — that latch only
    // matters outside the editor cheat.
    private void DebugAuditionDeathSequence()
    {
        PlayMonsterDeathSequence();
        if (MusicManager.Instance != null)
            MusicManager.Instance.ResumeGameMusic();
    }
#endif

    public void StartMinigame()
    {
        if (state != State.Idle) return;

        torchController.enableEnemyFlicker = true;

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
            // Don't route through SetLocked(true) here: its inverted semantics would
            // exit pointer lock (CursorLockMode.None) and the direct re-acquire on the
            // next line triggers Chrome's "Pointer lock cannot be acquired immediately
            // after the user has exited the lock" throttle. MinigameActive=true already
            // blocks player input; just freeze the cursor directly.
            GameManager.Instance.LOCKED = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (GameManager.Instance.cameraController != null)
            {
                GameManager.Instance.cameraController.lookAtTarget = monster3D;
                GameManager.Instance.cameraController.lookAtYOffset = monster3DAimHeight;
            }
        }

        if (sounds != null) sounds.PlayMonsterAppearStinger();
        PlayMonsterAppearRoar();
        StartIdleVocalsRoutine();

        StartCoroutine(RunSession());
    }

    private IEnumerator RunSession()
    {
        // Hold on the reveal before the first round so the appearance stinger + roar can breathe
        // and the player gets a moment to register the creature before gameplay starts.
        float settle = sounds != null ? Mathf.Max(0f, sounds.entranceSettleDelay) : 0f;
        if (settle > 0f)
        {
            titleText.text = string.Empty;
            yield return new WaitForSecondsRealtime(settle);
        }
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
                PlayMonsterHappyReaction();
                float happyHold = sounds != null ? Mathf.Max(0f, sounds.happyReactionHoldDuration) : 1.5f;
                yield return new WaitForSecondsRealtime(happyHold);
                PlayMonsterVanish();
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
                PlayMonsterDeathSequence();
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

            PlayMonsterUnhappyReaction();
            float unhappyHold = sounds != null ? Mathf.Max(0f, sounds.unhappyReactionHoldDuration) : 0f;
            if (unhappyHold > 0f)
                yield return new WaitForSecondsRealtime(unhappyHold);

            yield return new WaitForSecondsRealtime(postResponseDelay);
        }
    }

    private IEnumerator RunPlayerInput()
    {
        float seekerTime = 0f;
        float width = waveformContainer.rect.width;
        float endTime = currentDuration + hitTolerance;
        float speedMul = GetSeekerSpeedMultiplier();

        while (seekerTime < endTime)
        {
            seekerTime += Time.deltaTime * speedMul;
            seeker.anchoredPosition = new Vector2(TimeToX(seekerTime, width), 0f);

            foreach (Peak p in peaks)
            {
                if (!p.hit && !p.autoMarked && seekerTime > p.time + hitTolerance)
                {
                    p.autoMarked = true;
                    SpawnMarker(crossPrefab, p.time);
                }
            }

            bool interacted = interactAction != null && interactAction.WasPressedThisFrame();
            bool clicked = clickAction != null && clickAction.WasPressedThisFrame();
            bool spaced = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (clicked || spaced || interacted )
            {
                PlayRightHandClick();
                PlayPlayerSnap();
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
                PlayMonsterSyllable();
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

    private float GetSeekerSpeedMultiplier()
    {
        if (seekerSpeedByFailCount == null || seekerSpeedByFailCount.Length == 0)
            return 1f;
        int idx = Mathf.Clamp(failCount, 0, seekerSpeedByFailCount.Length - 1);
        return Mathf.Max(0.01f, seekerSpeedByFailCount[idx]);
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
        if (shuffledPairOrder.Count == 0) return null;

        if (shuffledPairCursor >= shuffledPairOrder.Count)
            shuffledPairCursor = 0;

        int idx = shuffledPairOrder[shuffledPairCursor];
        shuffledPairCursor++;
        return dialoguePairs[idx];
    }

    private void BuildShuffledPairOrder()
    {
        shuffledPairOrder.Clear();
        shuffledPairCursor = 0;
        if (dialoguePairs == null) return;
        for (int i = 0; i < dialoguePairs.Length; i++)
            shuffledPairOrder.Add(i);
        for (int i = shuffledPairOrder.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (shuffledPairOrder[i], shuffledPairOrder[j]) = (shuffledPairOrder[j], shuffledPairOrder[i]);
        }
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

    private void PlayMonsterHop()
    {
        if (sounds == null || sounds.monsterHop == null || !sounds.monsterHop.HasAnyClip)
            return;
        if (monster3D != null)
            sounds.monsterHop.PlayAttached(monster3D, Vector3.zero);
        else
            sounds.monsterHop.Play();
    }

    private void PlayMonsterHappyReaction()
    {
        if (sounds == null || sounds.monsterHappyReaction == null || !sounds.monsterHappyReaction.HasAnyClip)
            return;
        if (monster3D != null)
            sounds.monsterHappyReaction.PlayAttached(monster3D, Vector3.zero);
        else
            sounds.monsterHappyReaction.Play();
    }

    private void PlayMonsterVanish()
    {
        if (sounds == null || sounds.monsterVanish == null || !sounds.monsterVanish.HasAnyClip)
            return;
        // Use a baked world position (not PlayAttached) so the vanish cue keeps ringing out
        // after HidePanel() deactivates the monster3D GameObject a few lines below.
        if (monster3D != null)
            sounds.monsterVanish.PlayAt(monster3D.position);
        else
            sounds.monsterVanish.Play();
    }

    // Final-fail death beat: one stinger, fade the BGM out so the stinger and death sound
    // layers have the full mix, then trigger the whole death-sound array at once (the
    // MonsterMinigameSounds entries' per-clip Delay fields handle the timing within the array).
    private void PlayMonsterDeathSequence()
    {
        if (sounds == null) return;

        sounds.PlayMonsterDeathStinger();

        if (MusicManager.Instance != null)
            MusicManager.Instance.SuspendGameMusic(sounds.deathMusicFadeOutDuration);

        sounds.PlayMonsterDeathSounds();
    }

    private void PlayMonsterUnhappyReaction()
    {
        if (sounds == null || sounds.monsterUnhappyReaction == null || !sounds.monsterUnhappyReaction.HasAnyClip)
            return;
        if (monster3D != null)
            sounds.monsterUnhappyReaction.PlayAttached(monster3D, Vector3.zero);
        else
            sounds.monsterUnhappyReaction.Play();
    }

    private void PlayMonsterAppearRoar()
    {
        if (sounds == null || sounds.monsterAppearRoar == null || !sounds.monsterAppearRoar.HasAnyClip)
            return;
        if (monster3D != null)
            sounds.monsterAppearRoar.PlayAt(monster3DSpawnPos);
        else
            sounds.monsterAppearRoar.Play();
    }

    private void PlayMonsterSyllable()
    {
        if (sounds == null || sounds.monsterSyllables == null || !sounds.monsterSyllables.HasAnyClip)
            return;

        int instances = Mathf.Max(1, sounds.syllableInstancesPerTrigger);
        for (int i = 0; i < instances; i++)
        {
            if (monster3D != null)
                sounds.monsterSyllables.PlayAttached(monster3D, Vector3.zero);
            else
                sounds.monsterSyllables.Play();
        }
    }

    private void PlayMonsterIdleVocal()
    {
        if (sounds == null || sounds.monsterIdleVocals == null || !sounds.monsterIdleVocals.HasAnyClip)
            return;
        if (monster3D != null)
            sounds.monsterIdleVocals.PlayAttached(monster3D, Vector3.zero);
        else
            sounds.monsterIdleVocals.Play();
    }

    private void PlayPlayerSnap()
    {
        if (sounds == null || sounds.playerSnap == null || !sounds.playerSnap.HasAnyClip)
            return;

        int instances = Mathf.Max(1, sounds.snapInstancesPerTrigger);
        for (int i = 0; i < instances; i++)
            sounds.playerSnap.Play();
    }

    private void StartIdleVocalsRoutine()
    {
        StopIdleVocalsRoutine();
        if (sounds == null || sounds.monsterIdleVocals == null || !sounds.monsterIdleVocals.HasAnyClip)
            return;
        idleVocalsRoutine = StartCoroutine(RunIdleVocalsLoop());
    }

    private void StopIdleVocalsRoutine()
    {
        if (idleVocalsRoutine != null)
        {
            StopCoroutine(idleVocalsRoutine);
            idleVocalsRoutine = null;
        }
    }

    private IEnumerator RunIdleVocalsLoop()
    {
        float initial = sounds != null ? Mathf.Max(0f, sounds.idleVocalsInitialDelay) : 1.5f;
        if (initial > 0f)
            yield return new WaitForSecondsRealtime(initial);

        while (true)
        {
            float minI = sounds != null ? Mathf.Max(0f, sounds.idleVocalsMinInterval) : 2.5f;
            float maxI = sounds != null ? Mathf.Max(minI, sounds.idleVocalsMaxInterval) : 6f;
            float wait = UnityEngine.Random.Range(minI, maxI);
            yield return new WaitForSecondsRealtime(wait);

            // Skip this beat if the monster is mid-line or the session has ended.
            if (state == State.AlienPlaying || state == State.Idle)
                continue;

            PlayMonsterIdleVocal();
        }
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

        if (player != null)
        {
            // End spot sits on the straight line between the monster's spawn and the
            // player's position, monster3DApproachDistance metres shy of the player.
            // Using player.forward here would drift to the side whenever the player
            // isn't facing the spawn point.
            Vector3 toPlayer = player.position - monster3DSpawnPos;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            float approach = Mathf.Max(monster3DApproachDistanceFloor, monster3DApproachDistance);
            if (distance <= approach || distance < 0.0001f)
            {
                monster3DApproachPos = monster3DSpawnPos;
            }
            else
            {
                Vector3 dir = toPlayer / distance;
                monster3DApproachPos = player.position - dir * approach;
                monster3DApproachPos.y = monster3DSpawnPos.y;
            }
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
        // Reach monster3DApproachPos on the second-to-last fail so the final fail
        // (attack + game over) triggers with the monster already right in front of
        // the player. maxFails=3 → fail1=half, fail2=end. maxFails=2 → fail1=end.
        int stepDenom = Mathf.Max(1, maxFails - 1);
        float t = Mathf.Clamp01((float)failCount / stepDenom);
        Vector3 target = Vector3.Lerp(monster3DSpawnPos, monster3DApproachPos, t);

        KillMonster3DWalk();
        PlayMonster3DAnim(monster3DWalkState);
        PlayMonsterHop();
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

    private void HidePanel(bool keepMonster3DVisible = false)
    {
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        if (monster3D != null && !keepMonster3DVisible) monster3D.gameObject.SetActive(false);
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
        EndSession(won, fireEvent: true, releaseLock: true);
    }

    private void EndSession(bool won, bool fireEvent, bool releaseLock)
    {
        torchController.enableEnemyFlicker = false;
        StopIdleVocalsRoutine();
        KillMonster3DWalk();
        ClearRound();
        HidePanel(keepMonster3DVisible: !won);
        monster.localScale = monsterBaseScale;
        monster.localPosition = monsterBasePosition;

        if (GameManager.Instance != null && GameManager.Instance.cameraController != null)
            GameManager.Instance.cameraController.lookAtTarget = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.MinigameActive = false;
            if (releaseLock)
                GameManager.Instance.SetLocked(false);
        }

        IntensityManager.Instance.SetIntensity(0);

        state = State.Idle;
        if (fireEvent)
            OnMinigameEnded?.Invoke(won);
    }

    public void ForceStopForEndSequence()
    {
        if (state == State.Idle) return;
        StopAllCoroutines();
        EndSession(won: false, fireEvent: false, releaseLock: false);
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
