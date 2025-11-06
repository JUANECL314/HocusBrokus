using UnityEngine;
using System.Collections;

public class SecretTrigg : MonoBehaviour
{
    public float moveDistance = 3f;  // Distancia que sube la puerta
    public float moveSpeed = 2f;     // Velocidad del movimiento
    private bool doorOpen = false;
    private Transform door;

    void Start()
    {
        GameObject doorObj = GameObject.FindGameObjectWithTag("puertaBut");
        if (doorObj != null)
        {
            door = doorObj.transform;
        }
        else
        {
            Debug.LogWarning("No se encontró ningún objeto con el tag 'puertaBut'");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Si el objeto que entra tiene el tag "MainCamera"
        if (other.CompareTag("PLAYER") && !doorOpen)
        {
            StartCoroutine(OpenDoor());
        }
    }

    IEnumerator OpenDoor()
    {
        doorOpen = true;
        Vector3 startPos = door.position;
        Vector3 endPos = startPos + new Vector3(0, moveDistance, 0); // hacia arriba

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * moveSpeed;
            door.position = Vector3.Lerp(startPos, endPos, elapsed);
            yield return null;
        }
    }
}
