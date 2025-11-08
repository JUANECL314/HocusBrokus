using Photon.Pun;
using System.Collections;
using UnityEngine;

public class SpawnPoint : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;
    public GameObject playerPrefab;

    private bool playerSpawned = false;

    private void OnEnable()
    {
        // Si el jugador ya está en una sala (por ejemplo, el creador de la sala tras LoadLevel)
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Escena cargada y jugador ya en sala. Iniciando spawn...");
            StartCoroutine(SpawnPlayerWhenReady());
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom ejecutado. Preparando spawn...");
        StartCoroutine(SpawnPlayerWhenReady());
    }

    private IEnumerator SpawnPlayerWhenReady()
    {
        // Esperar a que Photon y la escena estén completamente listos
        while (!PhotonNetwork.IsConnectedAndReady || spawnPoints == null || spawnPoints.Length == 0)
        {
            yield return null;
        }

        // Esperar a que el prefab del jugador esté cargado en la escena
        while (playerPrefab == null)
        {
            yield return null;
        }

        if (playerSpawned)
        {
            Debug.LogWarning("Jugador ya instanciado, evitando duplicado.");
            yield break;
        }

        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Transform spawn = spawnPoints[index];

        Debug.Log($"Spawneando jugador {PhotonNetwork.LocalPlayer.NickName} en {spawn.name}");
        PhotonNetwork.Instantiate(playerPrefab.name, spawn.position, spawn.rotation);

        playerSpawned = true;
    }
}
