using UnityEngine;
using Photon.Pun; 

public class Levels : MonoBehaviourPunCallbacks
{
    public GameObject canvas;
    private void Start()
    {
        canvas.SetActive(false);
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        // Si el jugador entra al trigger
        Debug.Log("Entró: " + other.name);
        if (other.CompareTag("Player"))
        {
            

            // Solo el Master Client debe cargar la escena
            if (PhotonNetwork.IsMasterClient)
            {

                canvas.SetActive(true);
                
            }
        }
    }

    public void Level1_1Enter()
    {
        Debug.Log("El Master Client cargará la escena 'Cave'");
        PhotonNetwork.LoadLevel("CavePuzzle1");
    }
    public void Level1_3Enter()
    {
        Debug.Log("El Master Client cargará la escena 'Cave'");
        PhotonNetwork.LoadLevel("CavePuzzle3");
    }
}