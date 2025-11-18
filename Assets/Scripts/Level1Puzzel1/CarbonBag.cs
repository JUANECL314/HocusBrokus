using UnityEngine;

public class CarbonBag : MonoBehaviour
{
    public GameObject carbonPrefab;       // Prefab del carbón
    public Transform insideBagPoint;      // Punto dentro de la bolsa donde aparece
    public float interactDistance = 3f;   // Distancia para poder presionar E

    private Transform playerCamera;

    void Start()
    {
        playerCamera = Camera.main.transform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Ver si el jugador está mirando la bolsa
            if (IsLookingAtBag() && IsCloseEnough())
            {
                SpawnCarbonInside();
            }
        }
    }

    bool IsLookingAtBag()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        return Physics.Raycast(ray, out RaycastHit hit, interactDistance) && hit.transform == transform;
    }

    bool IsCloseEnough()
    {
        return Vector3.Distance(playerCamera.position, transform.position) <= interactDistance;
    }

    void SpawnCarbonInside()
    {
        Instantiate(carbonPrefab, insideBagPoint.position, insideBagPoint.rotation);
    }
}
