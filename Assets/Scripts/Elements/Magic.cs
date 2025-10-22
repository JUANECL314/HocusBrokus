using Photon.Pun;
using System.Collections;
using UnityEngine;
using static Elements;
public class Magic : MonoBehaviour
{
    public GameObject element;
    [SerializeField]
    private string elementPath = "Elements/";
    private Elements elementSelected;
    public Transform firePoint;

    private void Start()
    {
        if (element == null)
        {
            AssignElementToPlayer();
        }
        if (element != null)
        {
            elementSelected = element.GetComponent<Elements>();
        }
            
    }

    private void AssignElementToPlayer()
    {
        Elements.ElementType playerElementType = AssignElementByActorNumber();

        string prefabName = playerElementType.ToString();
        string fullPath = elementPath + prefabName;

        GameObject loaded = Resources.Load<GameObject>(fullPath);
        if (loaded != null) {
            element = loaded;
            Debug.Log($" Jugador {PhotonNetwork.LocalPlayer.ActorNumber} asignado al elemento {prefabName}");
        }
        else
        {
            Debug.LogError($" No se encontró el prefab Resource/{fullPath}");
        }

    }

    private ElementType AssignElementByActorNumber()
    {
        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % 4;
        return (ElementType)index;
    }
    public void elementDescription()
    {
        Debug.Log("Elemento seleccionado: " + (elementSelected != null ? elementSelected.idName : "(none)"));
        Debug.Log("Velocidad: " + (elementSelected != null ? elementSelected.velocityMov.ToString() : "-"));
        Debug.Log("Peso: " + (elementSelected != null ? elementSelected.weight.ToString() : "-"));
    }
    public void launchElement()
    {
        Debug.Log("Disparo");
        if (element == null)
        {
            Debug.LogWarning("Magic.launchElement: no element assigned.");
            return;
        }

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : (transform.position + transform.forward * 1.5f);
        Vector3 forward = (firePoint != null) ? firePoint.forward : transform.forward;

        GameObject spawned = PhotonNetwork.Instantiate(elementPath+element.name, spawnPos, Quaternion.LookRotation(forward));

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

        if (spawned.TryGetComponent(out PhotonView localView) && localView.IsMine)
        {
            StartCoroutine(DestroyAfterDelay(spawned,3f));
        }
        
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.Destroy(obj);
    }
}
