using System.Collections;
using UnityEngine;
using static Elements;

public class Magic1 : MonoBehaviour
{
    [Header("Prefab del elemento (opcional si usas Resources)")]
    public GameObject element;

    [Header("Carga por Resources (opcional)")]
    [SerializeField] private string elementPath = "Elements/"; // e.g. Resources/Elements/Fire
    [Tooltip("Tipo para el mock offline, se usará si 'element' está vacío")]
    public ElementType mockElementType = ElementType.Fire;

    [Header("Punto de disparo (opcional)")]
    public Transform firePoint;

    // cache info del prefab
    private Elements elementSelected;

    private void Start()
    {
        // Si no hay prefab asignado en el inspector, intenta cargar uno por Resources usando el tipo del mock
        if (element == null)
        {
            string prefabName = mockElementType.ToString();   // "Fire", "Water", etc.
            string fullPath = elementPath + prefabName;       // "Elements/Fire"
            GameObject loaded = Resources.Load<GameObject>(fullPath);
            if (loaded != null)
            {
                element = loaded;
                Debug.Log($"[Magic MOCK] Asignado prefab por Resources: {fullPath}");
            }
            else
            {
                Debug.LogError($"[Magic MOCK] No se encontró Resources/{fullPath}. Asigna 'element' en el Inspector o corrige la ruta.");
            }
        }

        if (element != null)
            elementSelected = element.GetComponent<Elements>();  // tiene idName, velocityMov, weight
    }

    public void elementDescription()
    {
        Debug.Log("Elemento seleccionado: " + (elementSelected ? elementSelected.idName : "(none)"));
        Debug.Log("Velocidad: " + (elementSelected ? elementSelected.velocityMov.ToString() : "-"));
        Debug.Log("Peso: " + (elementSelected ? elementSelected.weight.ToString() : "-"));
    }

    public void launchElement()
    {
        Debug.Log("[Magic MOCK] Disparo");

        if (element == null)
        {
            Debug.LogWarning("[Magic MOCK] launchElement: no hay 'element' asignado.");
            return;
        }

        // Posición y dirección de salida
        Vector3 spawnPos = firePoint ? firePoint.position : (transform.position + transform.forward * 1.5f);
        Vector3 forward = firePoint ? firePoint.forward : transform.forward;

        // ---- OFFLINE: Instantiate normal (sin Photon) ----
        GameObject spawned = Instantiate(element, spawnPos, Quaternion.LookRotation(forward));

        // Rigidbody (usa el que tenga o agrega uno)
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb == null) rb = spawned.AddComponent<Rigidbody>();

        // Config básica para que "avance" sin gravedad
        rb.useGravity = false;

        // Nota: si usas Unity clásico, las props correctas son 'drag' y 'velocity'.
        // Si en tu proyecto tienes extensiones que exponen 'linearDamping'/'linearVelocity',
        // mantenlas; de lo contrario, usa 'drag'/'velocity' estándar:
#if UNITY_6000_0_OR_NEWER
        // Si tu build expone linearDamping/linearVelocity, puedes dejar estas líneas:
        rb.linearDamping = 0f;
        rb.linearVelocity = forward.normalized * (elementSelected ? elementSelected.velocityMov : 10f);
#else
        rb.drag = 0f;
        rb.velocity = forward.normalized * (elementSelected ? elementSelected.velocityMov : 10f);
#endif

        // Destruir tras 3s en modo mock
        StartCoroutine(DestroyAfterDelay(spawned, 3f));
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(obj);
    }
}
