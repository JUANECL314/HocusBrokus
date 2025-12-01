using Photon.Pun;
using UnityEngine;

public class HiddenPuzzle : MonoBehaviour
{
    public Transform heightReference; // Objeto que determina la altura de activación
    private MeshRenderer[] meshRenderers;
    private Transform localPlayer;
    public GameObject padre;
    void Start()
    {
        if (heightReference == null)
        {
            Debug.LogError("Debe asignarse un Height Reference GameObject.");
            return;
        }

        // Obtener todos los MeshRenderer de los hijos
        meshRenderers = padre.GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length == 0)
            Debug.LogWarning("No se encontraron MeshRenderers en los hijos.");

        StartCoroutine(FindLocalPlayer());
    }

    System.Collections.IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
            yield return null;

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
    }

    void Update()
    {
        if (localPlayer == null || heightReference == null) return;

        // Comparar altura del jugador con la altura del objeto de referencia
        bool shouldHide = localPlayer.position.y > heightReference.position.y;
        
        foreach (var mr in meshRenderers)
        {
            if (mr != null)
                mr.enabled = shouldHide; // ocultar/mostrar visualmente
        }
    }
}
