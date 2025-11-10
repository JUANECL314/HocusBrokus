

using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SpawnPointLobby : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints; // Asigna varios puntos en el inspector
    public GameObject playerPrefab; // Prefab del jugador

    private bool playerSpawned = false;

    private void Start()
    {
        // Si ya está conectado y en una sala (por ejemplo, el creador)
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Jugador ya en sala al iniciar escena. Spawneando...");
            SpawnPlayer();
        }
    }

    // Este método se llama automáticamente cuando el jugador entra a la sala
    public override void OnJoinedRoom()
    {
        Debug.Log("Jugador se unió a la sala. Spawneando...");
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerSpawned)
        {
            Debug.LogWarning("Jugador ya instanciado. Ignorando duplicado.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No hay spawnPoints asignados en la escena.");
            return;
        }

        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Transform spawn = spawnPoints[index];

        Debug.Log($"Spawneando jugador {PhotonNetwork.LocalPlayer.NickName} en {spawn.name}");
        PhotonNetwork.Instantiate(playerPrefab.name, spawn.position, spawn.rotation);

        playerSpawned = true;
    }
}
