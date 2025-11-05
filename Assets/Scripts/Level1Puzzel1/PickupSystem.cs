using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    public Transform holdPoint;
    public float pickupRange = 3f;
    public float placeDistance = 3f;
    public LayerMask placeMask;
    public float smoothSpeed = 10f; // velocidad para mover suavemente el objeto

    private GameObject heldObject;
    private Rigidbody heldRb;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (heldObject == null)
                TryPickup();
            else
                TryPlace();
        }

        if (heldObject != null)
        {
            // Mover el objeto suavemente hacia el holdPoint
            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, holdPoint.position, Time.deltaTime * smoothSpeed);
            heldObject.transform.rotation = Quaternion.Slerp(heldObject.transform.rotation, holdPoint.rotation, Time.deltaTime * smoothSpeed);
        }
    }

    void TryPickup()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Pickup"))
            {
                heldObject = hit.collider.gameObject;
                heldRb = heldObject.GetComponent<Rigidbody>();

                heldRb.useGravity = false;
                heldRb.isKinematic = true;

                heldObject.transform.SetParent(holdPoint);
            }
        }
    }

    void TryPlace()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placeDistance, placeMask))
        {
            heldObject.transform.SetParent(null);
            heldObject.transform.position = hit.point;
            heldObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            heldRb.useGravity = true;
            heldRb.isKinematic = false;

            heldObject = null;
            heldRb = null;
        }
    }
}
