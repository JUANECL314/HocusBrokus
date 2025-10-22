using UnityEngine;
using Photon.Pun; 

public class Levels : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        // Si el jugador entra al trigger
        Debug.Log("Entró: " + other.name);
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador entró al trigger");

            // Solo el Master Client debe cargar la escena
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("El Master Client cargará la escena 'Cave'");
                PhotonNetwork.LoadLevel("CavePuzzle3");
            }
        }
    }
}