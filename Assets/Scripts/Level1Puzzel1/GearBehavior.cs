using System.Collections;
using UnityEngine;

public class GearBehavior : MonoBehaviour
{
    private Renderer rend;
    private Rigidbody rb;
    private bool isRotating = false;
    public bool isFalling = false;
    private bool destroyedDoors = false;
    private Vector3 initialPosition;

    public float rotationSpeed = 150f;
    public float timeToDestroyDoors = 20f;
    public float timeToAutoOff = 30f;
    public float fallSpeed = 2f; // velocidad máxima de caída

    void Start()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{name} no tiene Rigidbody");
            return;
        }

        rb.isKinematic = true; // inicial: control manual para que no se vuele
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        initialPosition = transform.position;
        rend.material.color = Color.gray;
    }

    void Update()
    {
        // Rotación
        if (isRotating)
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // Si está cayendo, aplicar gravedad limitada
        if (isFalling)
        {
            rb.isKinematic = false;
            rb.linearVelocity = new Vector3(0, -fallSpeed, 0); // caída controlada
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

        rend.material.color = Color.gray;
        yield return new WaitForSeconds(0.5f);
        rend.material.color = new Color(1f, 0.5f, 0f);
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
        // Agua → solo cambia color
        if (collision.gameObject.CompareTag("Water") && isRotating)
        {
            rend.material.color = Color.gray;
        }

        // Tierra → vuelve a posición inicial si estaba cayendo
        if (collision.gameObject.CompareTag("Earth") && isFalling)
        {
            StartCoroutine(ReturnToInitialPosition());
        }

        // Ground u otro objeto sólido → detener caída
        if (collision.gameObject.CompareTag("Ground") && isFalling)
        {
            isFalling = false;
            rb.isKinematic = true; // bloquea posición
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void MakeFall()
    {
        if (!isFalling)
        {
            isFalling = true;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    IEnumerator ReturnToInitialPosition()
    {
        isFalling = false;
        rb.isKinematic = true;

        Vector3 start = transform.position;
        float elapsed = 0f;
        float duration = 2f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, initialPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = initialPosition;
    }
}
