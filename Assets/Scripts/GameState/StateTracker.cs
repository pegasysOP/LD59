using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskType
{
    Batteries = 0,
    Signal = 1,
    SimonSays = 2,
}

public enum EndState
{
    InProgress = 0,
    Victory = 1,
    Lost = 2,
}

[DisallowMultipleComponent]
public class StateTracker : MonoBehaviour
{
    public static StateTracker Instance { get; private set; }

    [Header("Debug (read-only runtime view)")]
    [SerializeField] private bool startingDoorOpenedDebug;
    [SerializeField] private bool batteriesCompleteDebug;
    [SerializeField] private bool signalCompleteDebug;
    [SerializeField] private bool simonSaysCompleteDebug;
    [SerializeField] private EndState endStateDebug = EndState.InProgress;
    [SerializeField] private int encounterCountDebug;
    [SerializeField] private bool lastEncounterWonDebug;
    [SerializeField] private float lastEncounterTimeDebug = -1f;

    private readonly Dictionary<TaskType, bool> tasks = new Dictionary<TaskType, bool>
    {
        { TaskType.Batteries, false },
        { TaskType.Signal, false },
        { TaskType.SimonSays, false },
    };

    public bool StartingDoorOpened { get; private set; }
    public EndState CurrentEndState { get; private set; } = EndState.InProgress;
    public int AlienEncounterCount { get; private set; }
    public bool LastEncounterWon { get; private set; }
    public float LastEncounterTime { get; private set; } = -1f;
    public bool HasEncountered => AlienEncounterCount > 0;
    public float TimeSinceLastEncounter => HasEncountered ? Time.time - LastEncounterTime : float.PositiveInfinity;

    public event Action OnStartingDoorOpened;
    public event Action<TaskType> OnTaskCompleted;
    public event Action<EndState> OnEndStateChanged;
    public event Action<bool> OnAlienEncounterEnded;

    private Minigame subscribedMinigame;

    public bool AllTasksComplete
    {
        get
        {
            foreach (KeyValuePair<TaskType, bool> kvp in tasks)
                if (!kvp.Value) return false;
            return true;
        }
    }

    public bool IsTaskComplete(TaskType task)
    {
        return tasks.TryGetValue(task, out bool done) && done;
    }

    private void Awake()
    {
        Instance = this;

        subscribedMinigame = FindFirstObjectByType<Minigame>();
        if (subscribedMinigame != null)
            subscribedMinigame.OnMinigameEnded += HandleMinigameEnded;
    }

    private void OnDestroy()
    {
        if (subscribedMinigame != null)
            subscribedMinigame.OnMinigameEnded -= HandleMinigameEnded;

        if (Instance == this)
            Instance = null;
    }

    private void HandleMinigameEnded(bool won)
    {
        AlienEncounterCount++;
        LastEncounterWon = won;
        LastEncounterTime = Time.time;
        encounterCountDebug = AlienEncounterCount;
        lastEncounterWonDebug = won;
        lastEncounterTimeDebug = LastEncounterTime;

        OnAlienEncounterEnded?.Invoke(won);

        if (!won)
            TriggerLoss();
    }

    public void NotifyStartingDoorOpened()
    {
        if (StartingDoorOpened)
            return;

        StartingDoorOpened = true;
        startingDoorOpenedDebug = true;
        OnStartingDoorOpened?.Invoke();
    }

    public void CompleteTask(TaskType task)
    {
        if (tasks.TryGetValue(task, out bool done) && done)
            return;

        tasks[task] = true;
        UpdateTaskDebugMirror(task, true);
        OnTaskCompleted?.Invoke(task);
    }

    public void TriggerVictory()
    {
        SetEndState(EndState.Victory);
    }

    public void TriggerLoss()
    {
        SetEndState(EndState.Lost);
    }

    private void SetEndState(EndState next)
    {
        if (CurrentEndState != EndState.InProgress || next == EndState.InProgress)
            return;

        CurrentEndState = next;
        endStateDebug = next;
        OnEndStateChanged?.Invoke(next);
    }

    public void ResetState()
    {
        StartingDoorOpened = false;
        startingDoorOpenedDebug = false;

        foreach (TaskType task in new List<TaskType>(tasks.Keys))
        {
            tasks[task] = false;
            UpdateTaskDebugMirror(task, false);
        }

        CurrentEndState = EndState.InProgress;
        endStateDebug = EndState.InProgress;

        AlienEncounterCount = 0;
        encounterCountDebug = 0;
        LastEncounterWon = false;
        lastEncounterWonDebug = false;
        LastEncounterTime = -1f;
        lastEncounterTimeDebug = -1f;
    }

    private void UpdateTaskDebugMirror(TaskType task, bool done)
    {
        switch (task)
        {
            case TaskType.Batteries: batteriesCompleteDebug = done; break;
            case TaskType.Signal: signalCompleteDebug = done; break;
            case TaskType.SimonSays: simonSaysCompleteDebug = done; break;
        }
    }
}
