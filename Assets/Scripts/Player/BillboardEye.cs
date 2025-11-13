using UnityEngine;

public class BillboardEye : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Makes the eye face the camera
            transform.forward = Camera.main.transform.forward;
        }
    }
}
