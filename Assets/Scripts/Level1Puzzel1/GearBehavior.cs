using System.Collections;
using UnityEngine;

public class GearBehavior : MonoBehaviour
{
    private Renderer rend;
    private Rigidbody rb;
    private bool isRotating = false;
    private bool isFalling = false;
    private bool destroyedDoors = false;
    private Vector3 initialPosition;

    public float rotationSpeed = 150f;
    public float timeToDestroyDoors = 20f;
    public float timeToAutoOff = 30f;
    public float fallSpeed = 1f; // velocidad de caída controlada

    void Start()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{name} no tiene Rigidbody");
            return;
        }

        rb.isKinematic = true; // controla manualmente la posición para evitar que se vuele
        initialPosition = transform.position;
        rend.material.color = Color.gray;
    }

    void Update()
    {
        // Rotación mientras está activo
        if (isRotating)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }

        // Caída controlada
        if (isFalling)
        {
            Vector3 target = new Vector3(transform.position.x, transform.position.y - fallSpeed * Time.deltaTime, transform.position.z);
            transform.position = target;
        }
    }

    public void StartRotation()
    {
        if (!isRotating)
            StartCoroutine(RotateAndChangeColor());
    }

    IEnumerator RotateAndChangeColor()
    {
        isRotating = true;

        // Secuencia de color gris → naranja → rojo
        rend.material.color = Color.gray;
        yield return new WaitForSeconds(0.5f);
        rend.material.color = new Color(1f, 0.5f, 0f); // naranja
        yield return new WaitForSeconds(0.5f);
        rend.material.color = Color.red;

        StartCoroutine(DestroyDoorsAfterTime());
        StartCoroutine(AutoTurnOff());
    }

    IEnumerator DestroyDoorsAfterTime()
    {
        yield return new WaitForSeconds(timeToDestroyDoors);
        if (!destroyedDoors)
        {
            GameObject[] doors = GameObject.FindGameObjectsWithTag("Puerta");
            foreach (GameObject d in doors)
                Destroy(d);
            destroyedDoors = true;
        }
    }

    IEnumerator AutoTurnOff()
    {
        yield return new WaitForSeconds(timeToAutoOff);
        if (isRotating)
        {
            rend.material.color = Color.gray;
            isRotating = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 💧 Si recibe Water
        if (collision.gameObject.CompareTag("Water") && isRotating)
        {
            rend.material.color = Color.gray;
        }

        // 🌱 Si recibe Earth y está caído
        if (collision.gameObject.CompareTag("Earth") && isFalling)
        {
            StartCoroutine(ReturnToInitialPosition());
        }
    }

    // Método para hacer caer el engranaje
    public void MakeFall()
    {
        if (!isFalling)
        {
            isFalling = true;
            rb.isKinematic = true; // controlamos caída manualmente
        }
    }

    IEnumerator ReturnToInitialPosition()
    {
        isFalling = false;

        Vector3 start = transform.position;
        float elapsed = 0f;
        float duration = 2f; // tiempo para volver a la posición
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, initialPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = initialPosition;
    }
}
