using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerPickupDrop : MonoBehaviour
{
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private Transform objectGrabPointTransform;
    [SerializeField] private LayerMask pickUpLayerMask;

    private ObjectGrabable objectGrabable;
    private void Update()
    {
            if (Input.GetKeyDown(KeyCode.E))
        {
            if (objectGrabable == null)
            {


                float pickupDistance = 5f;
                if (Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycastHit, pickupDistance, pickUpLayerMask))
                {
                    if (raycastHit.transform.TryGetComponent(out objectGrabable))
                    {
                        objectGrabable.Grab(objectGrabPointTransform);
                    }
                }
            } else
            {
                objectGrabable.Drop();
                objectGrabable = null;
            }
        }

    }
}
