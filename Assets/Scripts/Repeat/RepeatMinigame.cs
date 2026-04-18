using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RepeatMinigame : MonoBehaviour
{
    [SerializeField]
    private GameObject[] SOSCharacters;

    //TODO: We may want these to be interactable or some other class 
    [SerializeField]
    private List<RepeatButton> buttons;

    [Header("Timing")]
    public float fastTime = 0.25f;
    public float slowTime = 0.6f;
    public float gapTime = 0.15f;

    public event Action<bool> OnMinigameEnded;

    private enum State { Idle, ShowingSequence, PlayerInput, RoundResolved, GameOver }
    private State state = State.Idle;

    private List<RepeatButton.Colour> sequence = new();
    private int playerIndex = 0;
    private int round = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var character in SOSCharacters)
        {
            character.SetActive(false);
        }

        foreach (var button in buttons)
        {
            button.OnPressed += HandleInput;
        }

        StartMinigame();
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

            yield return WaitForPlayerInput();

            if (playerIndex == -1)
            {
                state = State.GameOver;
                EndGame(false);
                yield break;
            }

            SOSCharacters[round].SetActive(true);

            round++;
            yield return new WaitForSeconds(0.5f);
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

        foreach (var colour in sequence)
        {
            RepeatButton btn = GetButtonByColour(colour);

            yield return FlashButton(btn, showTime);
            yield return new WaitForSeconds(gapTime);
        }
    }

    private float GetSpeed()
    {
        // SOS: fast, slow, fast
        if (round == 1) return slowTime;
        return fastTime;
    }

    private IEnumerator WaitForPlayerInput()
    {
        while (playerIndex >= 0 && playerIndex < sequence.Count)
        {
            yield return null;
        }
    }

    private void HandleInput(RepeatButton.Colour colour)
    {
        if (state != State.PlayerInput) return;

        if (sequence[playerIndex] == colour)
        {
            playerIndex++;
        }
        else
        {
            playerIndex = -1; // fail
        }
    }

    private RepeatButton GetButtonByColour(RepeatButton.Colour colour)
    {
        foreach (var b in buttons)
        {
            if (b.GetColour() == colour)
                return b;
        }
        return null;
    }

    private IEnumerator FlashButton(RepeatButton button, float duration)
    {
        // 👇 you can replace this with animation, emission, etc.
        Renderer r = button.GetComponent<Renderer>();

        if (r != null)
            r.material.EnableKeyword("_EMISSION");

        yield return new WaitForSeconds(duration);

        if (r != null)
            r.material.DisableKeyword("_EMISSION");
    }

    private void EndGame(bool won)
    {
        state = State.Idle;
        OnMinigameEnded?.Invoke(won);
    }
}
