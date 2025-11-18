using UnityEngine;

public class CarbonBag : MonoBehaviour
{
    public GameObject carbonPrefab;
    public Transform insideBagPoint;

    private GameObject currentCarbon;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SpawnOrReplaceCarbon();
        }
    }

    void SpawnOrReplaceCarbon()
    {
        // Si ya existe un carbón, destruirlo
        if (currentCarbon != null)
        {
            Destroy(currentCarbon);
        }

        // Generar un nuevo carbón
        currentCarbon = Instantiate(
            carbonPrefab,
            insideBagPoint.position,
            insideBagPoint.rotation,
            insideBagPoint
        );

        Debug.Log("🔥 Nuevo carbón generado");
    }
}
