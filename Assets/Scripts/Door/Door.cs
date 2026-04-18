using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    private bool isClosed = true;

    [SerializeField]
    private GameObject doorPanel;

    private const float OpenOffset = -1.45f;

    private const float OpenDuration = 0.35f;

    public void Interact()
    {
        Debug.Log("Interacting with door");
        if (isClosed && doorPanel != null)
        {
            StartCoroutine(MoveDoor(OpenOffset, OpenDuration));
        }
    }

    public bool IsInteractable()
    {
        return isClosed;
    }

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
}
