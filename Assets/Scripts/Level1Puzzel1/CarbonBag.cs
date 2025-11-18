using UnityEngine;

public class CarbonBag : MonoBehaviour
{
    public GameObject carbonPrefab;
    public Transform insideBagPoint;
    public float interactDistance = 3f;

    public Transform playerCamera;   // ASIGNAR A MANO

    private GameObject currentCarbon;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsLookingAtBag() && IsCloseEnough())
                TrySpawnCarbon();
        }
    }

    void TrySpawnCarbon()
    {
        if (currentCarbon != null) return;

        currentCarbon = Instantiate(carbonPrefab, insideBagPoint.position, insideBagPoint.rotation, insideBagPoint);
    }

    bool IsLookingAtBag()
    {
        if (playerCamera == null) return false;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        return Physics.Raycast(ray, out RaycastHit hit, interactDistance) && hit.transform == transform;
    }

    bool IsCloseEnough()
    {
        return Vector3.Distance(playerCamera.position, transform.position) <= interactDistance;
    }

    public void OnCarbonRemoved()
    {
        currentCarbon = null;
    }
}
