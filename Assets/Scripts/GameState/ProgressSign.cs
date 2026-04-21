using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ProgressSign : MonoBehaviour
{
    [Serializable]
    private class TaskLight
    {
        public TaskType task;
        public MeshRenderer meshRenderer;
    }

    [SerializeField]
    private List<TaskLight> taskLights = new List<TaskLight>
    {
        new TaskLight { task = TaskType.Batteries },
        new TaskLight { task = TaskType.SimonSays },
        new TaskLight { task = TaskType.RadarAlignment },
    };

    [SerializeField] private Material redMaterial;
    [SerializeField] private Material greenMaterial;

    [Tooltip("Log when event received. Turn on if lights not updating to confirm subscription is live.")]
    [SerializeField] private bool debugLogs;

    private bool subscribed;

    private void Start()
    {
        TrySubscribe();
        RefreshAllLights();
    }

    private void OnDestroy()
    {
        if (subscribed && StateTracker.Instance != null)
        {
            StateTracker.Instance.OnTaskCompleted -= HandleTaskCompleted;
            subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (StateTracker.Instance == null)
        {
            if (debugLogs) Debug.LogWarning("[ProgressSign] StateTracker.Instance null in Start — event not bound.", this);
            return;
        }

        StateTracker.Instance.OnTaskCompleted += HandleTaskCompleted;
        subscribed = true;
        if (debugLogs) Debug.Log("[ProgressSign] Subscribed to StateTracker.OnTaskCompleted.", this);
    }

    private void HandleTaskCompleted(TaskType task)
    {
        if (debugLogs) Debug.Log($"[ProgressSign] Task completed: {task}", this);
        SetLight(task, true);
    }

    private void RefreshAllLights()
    {
        bool hasTracker = StateTracker.Instance != null;
        foreach (TaskLight tl in taskLights)
        {
            bool complete = hasTracker && StateTracker.Instance.IsTaskComplete(tl.task);
            ApplyMaterial(tl.meshRenderer, complete);
        }
    }

    private void SetLight(TaskType task, bool complete)
    {
        foreach (TaskLight tl in taskLights)
        {
            if (tl.task == task)
                ApplyMaterial(tl.meshRenderer, complete);
        }
    }

    private void ApplyMaterial(MeshRenderer mr, bool complete)
    {
        if (mr == null) return;
        Material target = complete ? greenMaterial : redMaterial;
        if (target != null)
            mr.material = target;
    }
}
