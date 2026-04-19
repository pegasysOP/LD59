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
    private List<RepeatButton> buttons;

    [SerializeField]
    private GameObject approved;

    [Header("Timing")]
    public float fastTime = 0.3f; // How long each button flashes in fast phase
    public float slowTime = 1.0f; // How long each button flashes in slow phase
    public float flashGapTime = 0.25f; // Gap between flashes
    public float wrongInputPause = 0.5f; //Pause after replaying on wrong guess
    public float roundGapTime = 4f; //Gap between rounds after correct sequence

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

        foreach (RepeatButton button in buttons)
        {
            button.OnPressed += HandleInput;
        }

        approved.SetActive(false);
    }

    public void StartMinigame()
    {
        if (state != State.Idle)
            return;

        round = 0;
        StartCoroutine(RunSession());
    }

    private IEnumerator RunSession()
    {
        while (round < 3)
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
            SOSCharacters[round].SetActive(true);

            round++;
            yield return new WaitForSeconds(roundGapTime);
        }

        EndGame(true);
    }

    private void GenerateSequence()
    {
        sequence.Clear();

        for (int i = 0; i < 3; i++)
        {
            int rand = UnityEngine.Random.Range(0, buttons.Count);
            sequence.Add(buttons[rand].GetColour());
        }

        Debug.Log("Generated sequence: " + string.Join(", ", sequence));
    }

    private IEnumerator ShowSequence()
    {
        float showTime = GetSpeed();

        foreach (RepeatButton.Colour colour in sequence)
        {
            RepeatButton button = GetButtonByColour(colour);

            button.Flash(showTime);
            yield return new WaitForSeconds(showTime + flashGapTime);
        }
    }

    private float GetSpeed()
    {
        // SOS: fast, slow, fast
        if (round == 1)
            return slowTime;
        return fastTime;
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
