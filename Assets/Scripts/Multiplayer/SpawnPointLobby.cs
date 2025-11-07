using Photon.Pun;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public Transform[] spawnPoints; // Asigna varios puntos en el inspector
    public GameObject playerPrefab; // Prefab del jugador

    void Start()
    {
        // Si aún no hay un jugador local instanciado
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // Puedes hacer que cada jugador use un spawn distinto basado en su actor number
            int index = PhotonNetwork.LocalPlayer.ActorNumber-1;
           
            Transform spawn = spawnPoints[index];

            PhotonNetwork.Instantiate(playerPrefab.name, spawn.position, spawn.rotation);
        }
    }
}
