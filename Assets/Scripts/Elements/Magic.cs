using UnityEngine;

public class Magic : MonoBehaviour
{
    public GameObject element;
    Elements elementSelected;
    public Transform firePoint;

    private void Start()
    {
        if (element != null)
            elementSelected = element.GetComponent<Elements>();
    }

    public void elementDescription()
    {
        Debug.Log("Elemento seleccionado: " + (elementSelected != null ? elementSelected.idName : "(none)"));
        Debug.Log("Velocidad: " + (elementSelected != null ? elementSelected.velocityMov.ToString() : "-"));
        Debug.Log("Peso: " + (elementSelected != null ? elementSelected.weight.ToString() : "-"));
    }
    public void launchElement()
    {
        if (element == null)
        {
            Debug.LogWarning("Magic.launchElement: no element assigned.");
            return;
        }

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : (transform.position + transform.forward * 1.5f);
        Vector3 forward = (firePoint != null) ? firePoint.forward : transform.forward;

        GameObject spawned = Instantiate(element, spawnPos, Quaternion.LookRotation(forward));

        // NOTE: removed runtime tag assignment.
        // Set the tag on the element prefab in the Inspector (root GameObject) so the spawned object inherits it.

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb == null) rb = spawned.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.linearDamping = 0f;

        if (elementSelected != null)
        {
            rb.mass = elementSelected.weight;
            rb.linearVelocity = forward.normalized * elementSelected.velocityMov;
        }
        else
        {
            rb.linearVelocity = forward.normalized * 10f;
        }

        Destroy(spawned, 3f);
    }
}
