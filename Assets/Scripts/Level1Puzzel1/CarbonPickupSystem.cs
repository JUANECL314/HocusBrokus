using UnityEngine;

public class CarbonPickupSystem : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform holdPoint;
    public float pickupDistance = 3f;

    private CarbonItem heldCarbon;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldCarbon == null)
                TryPickup();
            else
                DropCarbon();
        }

        if (heldCarbon != null)
        {
            heldCarbon.transform.position = holdPoint.position;
            heldCarbon.transform.rotation = holdPoint.rotation;
        }
    }

    void TryPickup()
    {
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, pickupDistance))
        {
            // Solo levantar objetos con el tag Carbon
            if (hit.collider.CompareTag("Carbon"))
            {
                if (hit.collider.TryGetComponent(out CarbonItem carbon))
                {
                    heldCarbon = carbon;
                    heldCarbon.PickUp(holdPoint);
                }
            }
        }
    }

    void DropCarbon()
    {
        heldCarbon.Drop();
        heldCarbon = null;
    }
}
