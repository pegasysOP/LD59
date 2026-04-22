using UnityEngine;

public class ViewModelCameraFollow : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    void LateUpdate()
    {
        //transform.position = mainCamera.transform.position;
        transform.rotation = mainCamera.transform.rotation;
    }
}
