

using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SpawnPointLobby : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints; // Asigna varios puntos en el inspector
    public GameObject playerPrefab; // Prefab del jugador
    string _salaLocal = "Lobby";
    public static Transform[] SpawnPointsStatic;
    private bool playerSpawned = false;
    void Awake()
    {
        SpawnPointsStatic = spawnPoints;
    }

    


    private void Start()
    {
        

        string nombreEscena = SceneManager.GetActiveScene().name;
        // Si ya está conectado y en una sala (por ejemplo, el creador)
        if (!PhotonNetwork.InRoom && nombreEscena  == _salaLocal)
        {
            AparicionLobbyIndividual();
        }
        else if(PhotonNetwork.InRoom && nombreEscena != _salaLocal)
        {
            SpawnPlayer();
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

    void AparicionLobbyIndividual()
    {
        Vector3 spawnPoint = transform.position;

        // Instanciar el jugador offline
        GameObject jugador = Instantiate(playerPrefab, spawnPoint, Quaternion.identity);


        jugador.GetComponent<PlayerMagicInput>().enabled = false;
        jugador.GetComponent<Magic>().enabled = false;
        jugador.GetComponent<Elements>().enabled = false;

        // Activar cámara y audio
        PlayerCamera pc = jugador.GetComponent<PlayerCamera>();
        if (pc != null) pc.EnableLocalCamera(); // Método que activa camera y audio en jugador local
    }

    public override void OnLeftRoom()
    {
        playerSpawned = false;
    }
}
