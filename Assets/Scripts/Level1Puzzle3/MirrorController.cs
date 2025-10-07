using UnityEngine;

public class MirrorController : MonoBehaviour
{
    public float rotationStep = 1f;

    public void RotateLeft()
    {
        transform.Rotate(Vector3.up, -rotationStep);
    }

    public void RotateRight()
    {
        transform.Rotate(Vector3.up, rotationStep);
    }
}
