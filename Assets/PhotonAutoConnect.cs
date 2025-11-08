using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement; // 👈 Necesario para cargar escenas
using TMPro;
using UnityEngine.UI;
public class PhotonAutoConnect : MonoBehaviourPunCallbacks
{
    
    
    string roomName = "";

    
    byte maxPlayers = 4;

    public Button connectButton;

    string sceneToLoad = "";
    public TMP_Dropdown sceneDropdown;

    void Start()
    {
        Debug.Log("Conectando a Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;
        if (connectButton != null) connectButton.onClick.AddListener(CargarEscena);
    }


    void CargarEscena()
    {
        sceneToLoad = sceneDropdown.options[sceneDropdown.value].text;
        roomName = $"Nivel_{sceneDropdown.options[sceneDropdown.value].text}";
        PhotonNetwork.ConnectUsingSettings();
    }
    // Llamado cuando se conecta al servidor maestro de Photon
    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al servidor maestro de Photon.");
        PhotonNetwork.JoinLobby();
    }


    
    public override void OnJoinedLobby()
    {



        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }
    // Si no hay salas disponibles, se crea una nueva
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("⚠No se encontró sala disponible. Creando una nueva...");
        Debug.Log(roomName);
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
            Debug.Log($"Cargando escena: {sceneToLoad}");
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
