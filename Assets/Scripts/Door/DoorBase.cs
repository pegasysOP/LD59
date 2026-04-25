using System.Collections;
using UnityEngine;

public class DoorBase : MonoBehaviour, IInteractable
{
    [SerializeField]
    protected GameObject doorPanel;

    protected bool isClosed = true;
    protected float initialTimer = 0f;

    [SerializeField]
    protected MeshRenderer meshRenderer;

    [SerializeField]
    protected Material greenMaterial;
    [SerializeField]
    protected Material redMaterial;

    protected const float OpenOffset = -1.45f;

    public virtual void Interact()
    {
        throw new System.NotImplementedException();
    }

    public virtual bool IsInteractable()
    {
        if ((isClosed && initialTimer <= 0) == true)
        {
            meshRenderer.material = redMaterial;
        }
        return isClosed && initialTimer <= 0;
    }

    protected virtual void OnDoorOpened()
    {
        StateTracker.Instance?.NotifyStartingDoorOpened();
    }

    protected IEnumerator MoveDoor(float deltaZ, float duration)
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

        meshRenderer.material = redMaterial;

        OnDoorOpened();
    }
}
