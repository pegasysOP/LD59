using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RepeatMinigame : MonoBehaviour
{
    [SerializeField]
    private GameObject[] SOSCharacters;

    [SerializeField]
    private GameObject[] loginText;

    [SerializeField]
    private List<RepeatButton> buttons;

    [SerializeField]
    private GameObject approved;

    [SerializeField]
    private RepeatMinigameSounds sounds;

    /// <summary>Shared Simon SFX config (used by <see cref="StartMinigameButton"/> for the start cue).</summary>
    public RepeatMinigameSounds SoundConfig => sounds;

    [Header("Timing")]
    public float fastTime = 0.3f; // How long each button flashes in fast phase
    public float slowTime = 1.0f; // How long each button flashes in slow phase
    public float flashGapTime = 0.25f; // Gap between flashes
    public float wrongInputPause = 0.5f; //Pause after replaying on wrong guess
    public float roundGapTime = 4f; //Gap between rounds after correct sequence

    // Delay before the first round begins when the minigame starts
    public float initialDelay = 1f;

    public int rounds = 3;

    public int sequenceLength = 3;

    private float intensityOnFail = 0.25f;

    public event Action<bool> OnMinigameEnded;

    private enum State { Idle, ShowingSequence, PlayerInput, RoundResolved, GameOver }
    private State state = State.Idle;

    private List<RepeatButton.Colour> sequence = new();
    private int playerIndex = 0;
    private int round = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (GameObject character in SOSCharacters)
        {
            character.SetActive(false);
        }

        foreach (GameObject text in loginText)
        {
            text.SetActive(false);
        }

        foreach (RepeatButton button in buttons)
        {
            button.OnPressed += HandleInput;
        }

        approved.SetActive(false);
    }

    public void StartMinigame()
    {
        foreach (GameObject text in loginText)
        {
            text.SetActive(true);
        }

        if (state != State.Idle)
            return;

        round = 0;

        SetButtonInteractible(true);
        StartCoroutine(RunSession());
    }

    private IEnumerator RunSession()
    {
        // Initial delay before first round so player has a moment to prepare
        //if (initialDelay > 0f)
        //    yield return new WaitForSeconds(initialDelay);

        while (round < rounds)
        {
            SetButtonInteractible(true);
            GenerateSequence();

            state = State.ShowingSequence;
            yield return ShowSequence();

            state = State.PlayerInput;
            playerIndex = 0;

            //This "pauses" execution of the rest of RunSession until the player has guessed 3 times
            yield return WaitForPlayerInput();

            //After this check for game loss
            if (playerIndex == -1)
            {
                state = State.GameOver;
                EndGame(false);
                yield break;
            }

            //If we get here then they won so set the next SOS character and increment the rounds
            //TODO: We may want to replace this with an animation or different visual effects
            if (SOSCharacters.Length > round)
            {
                foreach(GameObject character in SOSCharacters)
                {
                    character.SetActive(false);
                    loginText[1].SetActive(false);
                }
                SOSCharacters[round].SetActive(true);
            }
               
            round++;
            sequenceLength++;
            SetButtonInteractible(false);

            if (round == 3)
            {
                Vector3 successPos = approved != null ? approved.transform.position : transform.position;
                PlayMinigameSuccessAt(successPos);
            }
            yield return new WaitForSeconds(roundGapTime);
        }

        EndGame(true);
    }

    private void GenerateSequence()
    {
        sequence.Clear();

        for (int i = 0; i < sequenceLength; i++)
        {
            int rand = UnityEngine.Random.Range(0, buttons.Count);
            sequence.Add(buttons[rand].GetColour());
        }

        Debug.Log("Generated sequence: " + string.Join(", ", sequence));
    }

    private IEnumerator ShowSequence()
    {
        SetButtonInteractible(false);
        foreach (RepeatButton.Colour colour in sequence)
        {
            RepeatButton button = GetButtonByColour(colour);

            PlayButtonPressAtButton(button);
            button.Flash(slowTime);
            yield return new WaitForSeconds(slowTime + flashGapTime);
        }
        SetButtonInteractible(true);
    }

    private IEnumerator WaitForPlayerInput()
    {
        while (playerIndex < sequence.Count)
        {
            yield return null;
        }
    }

    private void HandleInput(RepeatButton.Colour colour)
    {
        if (state != State.PlayerInput)
            return;

        RepeatButton pressed = GetButtonByColour(colour);
        PlayButtonPressAtButton(pressed);

        if (sequence[playerIndex] == colour)
        {
            playerIndex++;

            if (playerIndex < sequence.Count)
                PlaySuccessfulEntryAtButton(pressed);
        }
        else
        {
            if (isReplaying)
                return;

            PlaySequenceFailAtButton(pressed);

            playerIndex = 0;
            isReplaying = true;
            state = State.ShowingSequence;
            IntensityManager.Instance.AddIntensity(intensityOnFail);
            StartCoroutine(ReplaySequence());
        }
    }

    private bool isReplaying = false;

    private IEnumerator ReplaySequence()
    {
        yield return new WaitForSeconds(wrongInputPause);
        yield return ShowSequence();
        state = State.PlayerInput;

        isReplaying = false;
    }

    private RepeatButton GetButtonByColour(RepeatButton.Colour colour)
    {
        foreach (RepeatButton b in buttons)
        {
            if (b.GetColour() == colour)
                return b;
        }
        return null;
    }

    private void SetButtonInteractible(bool isInteractible)
    {
        foreach (RepeatButton b in buttons)
        {
            b.isInteractable = isInteractible;
        }
    }

    private void EndGame(bool won)
    {
        state = State.Idle;
        OnMinigameEnded?.Invoke(won);
        if (won)
        {
            foreach (RepeatButton b in buttons)
            {
                b.isInteractable = false;
            }
            StateTracker.Instance?.CompleteTask(TaskType.SimonSays);

            Vector3 successPos = approved != null ? approved.transform.position : transform.position;
            

            approved.SetActive(true);
        }
    }

    private void PlaySfxPositional(AudioClipVolume perceived, Vector3 worldPosition, float pitch = 1f)
    {
        if (sounds == null || perceived == null || perceived.Clip == null || AudioManager.Instance == null)
            return;

        float linear = AudioVolume.ToLinear(perceived.Volume);
        var shaped = new AudioClipVolume(perceived.Clip, linear, perceived.Delay);
        AudioManager.Instance.PlaySfxAtPoint(shaped, pitch, worldPosition);
    }

    private void PlayButtonPressAtButton(RepeatButton button)
    {
        if (button == null || sounds == null || sounds.buttonPressByButtonIndex == null ||
            sounds.buttonPressByButtonIndex.Count == 0)
            return;

        int idx = buttons.IndexOf(button);
        if (idx < 0)
            return;

        int clipIdx = Mathf.Clamp(idx, 0, sounds.buttonPressByButtonIndex.Count - 1);
        AudioClipVolume cv = sounds.buttonPressByButtonIndex[clipIdx];
        PlaySfxPositional(cv, button.transform.position);
    }

    private void PlaySequenceFailAtButton(RepeatButton button)
    {
        if (button == null)
            return;
        PlaySfxPositional(sounds?.sequenceFail, button.transform.position);
    }

    private void PlaySuccessfulEntryAtButton(RepeatButton button)
    {
        if (button == null)
            return;
        //PlaySfxPositional(sounds?.successfulEntry, button.transform.position);
    }

    private void PlayMinigameSuccessAt(Vector3 worldPosition)
    {
        if (sounds == null || sounds.minigameSuccess == null || !sounds.minigameSuccess.HasAnyClip)
            return;
        sounds.minigameSuccess.PlayAt(worldPosition);
    }
}
