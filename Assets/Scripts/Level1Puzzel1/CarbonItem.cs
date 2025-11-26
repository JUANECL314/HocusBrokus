using UnityEngine;

public class CarbonItem : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void PickUp(Transform holdPoint)
    {
        rb.useGravity = false;
        rb.isKinematic = true;

        transform.SetParent(holdPoint);
    }

    public void Drop()
    {
        transform.SetParent(null);

        rb.useGravity = true;
        rb.isKinematic = false;
    }
}
