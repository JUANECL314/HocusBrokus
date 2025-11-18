using UnityEngine;

public class CarbonItem : MonoBehaviour
{
    private Rigidbody rb;
    private CarbonBag parentBag;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Si el carbón aparece dentro de la bolsa, guardamos la bolsa
        parentBag = GetComponentInParent<CarbonBag>();
    }

    public void PickUp(Transform holdPoint)
    {
        // Avisar a la bolsa que el carbón ya no está
        if (parentBag != null)
        {
            parentBag.OnCarbonRemoved();
            parentBag = null;
        }

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
