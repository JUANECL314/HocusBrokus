using Photon.Pun;
using UnityEngine;

public class SpawnPointLobby : MonoBehaviour
{
    public Transform[] spawnPoints; // Asigna varios puntos en el inspector
    public GameObject playerPrefab; // Prefab del jugador

    public static Transform[] SpawnPointsStatic;

    void Awake()
    {
        SpawnPointsStatic = spawnPoints;
    }

    void Start()
    {
        // Si aún no hay un jugador local instanciado
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // Puedes hacer que cada jugador use un spawn distinto basado en su actor number
            int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;

            Transform spawn = spawnPoints[index];

            PhotonNetwork.Instantiate(playerPrefab.name, spawn.position, spawn.rotation);
        }
    }

    // Helper to get a spawn for a given actor number (wraps array length)
    public static Transform GetSpawnForActor(int actorNumber)
    {
        if (SpawnPointsStatic == null || SpawnPointsStatic.Length == 0) return null;
        int index = (actorNumber - 1) % SpawnPointsStatic.Length;
        if (index < 0) index += SpawnPointsStatic.Length;
        return SpawnPointsStatic[index];
    }
    
}
