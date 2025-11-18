using UnityEngine;

public class CarbonBag : MonoBehaviour
{
    public GameObject carbonPrefab;
    public Transform insideBagPoint;
    public float interactDistance = 3f;

    private Transform playerCamera;
    private GameObject currentCarbon;

    void Update()
    {
        if (playerCamera == null)
        {
            FindPlayerCamera();
            if (playerCamera == null)
            {
                Debug.Log("❌ No encontré la cámara");
                return;
            }
            else
            {
                Debug.Log("✅ Cámara encontrada: " + playerCamera.name);
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("➡️ Presionaste E");

            if (IsLookingAtBag())
                Debug.Log("👀 Sí está mirando la bolsa");
            else
                Debug.Log("❌ NO está mirando la bolsa");

            if (IsCloseEnough())
                Debug.Log("👣 Sí está lo suficientemente cerca");
            else
                Debug.Log("❌ Está muy lejos");

            if (IsLookingAtBag() && IsCloseEnough())
                TrySpawnCarbon();
        }
    }

    void FindPlayerCamera()
    {
        GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (camObj != null)
            playerCamera = camObj.transform;
    }

    void TrySpawnCarbon()
    {
        if (currentCarbon != null)
        {
            Debug.Log("⚠️ Ya hay carbón dentro");
            return;
        }

        Debug.Log("🟢 ¡Generando Carbon!");
        currentCarbon = Instantiate(carbonPrefab, insideBagPoint.position, insideBagPoint.rotation, insideBagPoint);
    }

    bool IsLookingAtBag()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            Debug.Log("🔵 Raycast hit: " + hit.transform.name);
            return hit.transform == transform;
        }

        Debug.Log("🔴 Raycast NO golpeó nada");
        return false;
    }

    bool IsCloseEnough()
    {
        float dist = Vector3.Distance(playerCamera.position, transform.position);
        Debug.Log("📏 Distancia: " + dist);
        return dist <= interactDistance;
    }
}
