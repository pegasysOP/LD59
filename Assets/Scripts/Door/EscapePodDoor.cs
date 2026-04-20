using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class EscapePodDoor : DoorBase
{

    private Keyboard keyboard = Keyboard.current;

    [SerializeField]
    private GameObject doorPanel;

    private const float OpenOffset = -1.45f;

    private const float OpenDuration = 0.35f;

    public override void Interact()
    {
        Debug.Log("Interacting with escape pod door");
        if (isClosed && doorPanel != null)
        {
            StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
        }
    }

    public override bool IsInteractable()
    {
        //return true;

        return StateTracker.Instance.AllTasksComplete;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private IEnumerator MoveDoor(float deltaZ, float duration)
    {
        isClosed = false;

        Transform t = doorPanel.transform;
        Vector3 start = t.localPosition;
        Vector3 target = start + new Vector3(0f, 0f, deltaZ);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float frac = Mathf.Clamp01(elapsed / duration);
            t.localPosition = Vector3.Lerp(start, target, frac);
            yield return null;
        }

        t.localPosition = target;

        StateTracker.Instance?.NotifyStartingDoorOpened();
    }

    public void OpenDoorEndCutscene()
    {
        StartCoroutine(MoveDoor(-OpenOffset, OpenDuration));
    }

    private void Update()
    {
        //FIXME: We may want to replace this but for now just open escape hatch door after all tasks complete
        //if (StateTracker.Instance.AllTasksComplete)
        //{
        //    StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
        //}

        if (StateTracker.Instance.AllTasksComplete)
        {
            meshRenderer.material = greenMaterial;
        }

        if (keyboard.zKey.wasPressedThisFrame)
        {
            StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
        }
    }

}
