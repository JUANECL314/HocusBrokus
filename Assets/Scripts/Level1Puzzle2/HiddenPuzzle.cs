using Photon.Pun;
using System.Collections;
using UnityEngine;

public class HiddenPuzzle : MonoBehaviour
{

    public GameObject padre;          // Padre de los objetos a ocultar
    public MeshRenderer[] meshRenderers;

        
    private bool lastShouldHide = false; // Para actualizar solo si cambia el estado

    void Start()
    {
     
        if(padre != null) meshRenderers = padre.GetComponentsInChildren<MeshRenderer>();
        
    }

    

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        foreach (var mr in meshRenderers)
        {
            if (mr != null)
                mr.enabled = false;
        }

        lastShouldHide = false;
    }

    private void OnTriggerExit(Collider other)
    {

        if (!other.CompareTag("Player")) return;
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        foreach (var mr in meshRenderers)
        {
            if (mr != null)
                mr.enabled = true;
        }

        lastShouldHide = true;
    }
}

