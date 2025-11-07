using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement; // 👈 Necesario para cargar escenas

public class PhotonAutoConnect : MonoBehaviourPunCallbacks
{
    [Header("Configuración de conexión")]
    
    public string roomName = "";

    
    public byte maxPlayers = 4;

    [Header("Escena a cargar al unirse a la sala")]
    
    public string sceneToLoad = ""; 

    void Start()
    {
        Debug.Log("🔌 Conectando a Photon...");
        PhotonNetwork.AutomaticallySyncScene = true; 
        PhotonNetwork.ConnectUsingSettings();
    }

    // Llamado cuando se conecta al servidor maestro de Photon
    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al servidor maestro de Photon.");
        PhotonNetwork.JoinRandomRoom(); 
    }

    // Si no hay salas disponibles, se crea una nueva
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("⚠No se encontró sala disponible. Creando una nueva...");

        string newRoomName = string.IsNullOrEmpty(roomName) ? "Sala_" + Random.Range(1000, 9999) : roomName;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers
        };

        PhotonNetwork.CreateRoom(newRoomName, options);
    }

    
    public override void OnJoinedRoom()
    {
        Debug.Log($"Se ha unido a la sala: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Jugadores en la sala: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

        // 👇 Cargar la escena automáticamente
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"🌍 Cargando escena: {sceneToLoad}");
            PhotonNetwork.LoadLevel(sceneToLoad); 
        }
        else
        {
            Debug.LogWarning("No se asignó ninguna escena para cargar.");
        }
    }

    // Llamado si la conexión falla
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Desconectado de Photon. Motivo: {cause}");
    }
}
