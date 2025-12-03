using Photon.Pun;
using System.Collections;
using UnityEngine;

public class HiddenPuzzle : MonoBehaviour
{
    public Transform heightReference; // Objeto que determina la altura de activación
    public GameObject padre;          // Padre de los objetos a ocultar
    public MeshRenderer[] meshRenderers;
    private Transform localPlayer;
        
    private bool lastShouldHide = false; // Para actualizar solo si cambia el estado

    void Start()
    {
        if (heightReference == null)
        {
            Debug.LogError("Debe asignarse un Height Reference GameObject.");
            return;
        }
        if(padre != null) meshRenderers = padre.GetComponentsInChildren<MeshRenderer>();
        StartCoroutine(FindLocalPlayer());
    }

    IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
            yield return null;

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
    }

    void Update()
    {
        if (localPlayer == null || heightReference == null || padre == null)
            return;

       

        // Comparar altura del jugador con la altura del objeto de referencia
        bool shouldHide = localPlayer.position.y > heightReference.position.y;

        // Solo actualizar si cambió el estado
        if (shouldHide != lastShouldHide)
        {
            foreach (var mr in meshRenderers)
            {
                if (mr != null)
                    mr.enabled = shouldHide;
            }

            lastShouldHide = shouldHide;
        }
    }
}
