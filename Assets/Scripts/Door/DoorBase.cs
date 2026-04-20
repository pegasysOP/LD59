using UnityEngine;

public class DoorBase : MonoBehaviour, IInteractable
{
    protected bool isClosed = true;
    protected float initialTimer = 0f;

    [SerializeField]
    protected MeshRenderer meshRenderer;

    [SerializeField]
    protected Material greenMaterial;
    [SerializeField]
    protected Material redMaterial;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
