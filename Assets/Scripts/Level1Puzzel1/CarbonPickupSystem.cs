using UnityEngine;

public class CarbonPickupSystem : MonoBehaviour
{
    public Transform cameraTransform;      // cámara del jugador
    public Transform holdPoint;            // donde se sostiene el carbón
    public float pickupDistance = 3f;      // distancia para agarrar
    public LayerMask carbonLayer;          // layer del carbón

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

        // Si estamos cargando carbón → moverlo al holdPoint
        if (heldCarbon != null)
        {
            heldCarbon.transform.position = holdPoint.position;
            heldCarbon.transform.rotation = holdPoint.rotation;
        }
    }

    void TryPickup()
    {
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, pickupDistance, carbonLayer))
        {
            if (hit.collider.TryGetComponent(out CarbonItem carbon))
            {
                heldCarbon = carbon;
                heldCarbon.PickUp(holdPoint);
            }
        }
    }

    void DropCarbon()
    {
        heldCarbon.Drop();
        heldCarbon = null;
    }
}
