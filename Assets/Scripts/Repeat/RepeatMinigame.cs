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

        foreach (RepeatButton button in buttons)
        {
            button.isInteractable = true;
        }
        StartCoroutine(RunSession());
    }

    private IEnumerator RunSession()
    {
        // Initial delay before first round so player has a moment to prepare
        //if (initialDelay > 0f)
        //    yield return new WaitForSeconds(initialDelay);

        while (round < rounds)
        {
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
                SOSCharacters[round].SetActive(true);

            round++;
            sequenceLength++;
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
        foreach (RepeatButton.Colour colour in sequence)
        {
            RepeatButton button = GetButtonByColour(colour);

            button.Flash(slowTime);
            yield return new WaitForSeconds(slowTime + flashGapTime);
        }
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

        if (sequence[playerIndex] == colour)
        {
            playerIndex++;
        }
        else
        {
            if (isReplaying) 
                return;

            playerIndex = 0;
            isReplaying = true;
            state = State.ShowingSequence;
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

            approved.SetActive(true);   
        }
    }
}
