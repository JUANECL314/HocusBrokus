using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotonAutoConnect : MonoBehaviourPunCallbacks
{
    string roomName = "";
    byte maxPlayers = 4;

    public Button crearButton;
    public Button unirButton;
    public TMP_Dropdown sceneDropdown;

    string sceneToLoad = "";
    private bool enLobby = false;

    void Start()
    {
        Debug.Log("Conectando a Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;

        if (crearButton != null) crearButton.onClick.AddListener(Crear);
        if (unirButton != null) unirButton.onClick.AddListener(Unir);

        crearButton.interactable = false;
        unirButton.interactable = false;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Conectado al servidor maestro de Photon.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        enLobby = true;
        Debug.Log("✅ Unido al lobby.");
        crearButton.interactable = true;
        unirButton.interactable = true;
    }

    void Crear()
    {
        if (!enLobby)
        {
            Debug.LogWarning("Aún no estás en el lobby.");
            return;
        }

        sceneToLoad = sceneDropdown.options[sceneDropdown.value].text;
        roomName = $"Nivel_{sceneToLoad}";
        CreateRoom(roomName);
    }

    void Unir()
    {
        if (!enLobby)
        {
            Debug.LogWarning("Aún no estás en el lobby.");
            return;
        }

        sceneToLoad = sceneDropdown.options[sceneDropdown.value].text;
        roomName = $"Nivel_{sceneToLoad}";
        Debug.Log($"Intentando unirse a la sala: {roomName}");
        JoinRoom(roomName);
    }

    public void CreateRoom(string nombre)
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers
        };

        PhotonNetwork.CreateRoom(nombre, options);
    }

    public void JoinRoom(string nombre)
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        PhotonNetwork.JoinRoom(nombre);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"🎮 Se ha unido a la sala: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Jugadores en sala: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"Cargando escena: {sceneToLoad}");
            PhotonNetwork.LoadLevel(sceneToLoad);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"⚠ Falló JoinRoom ({message}), creando la sala {roomName}...");
        CreateRoom(roomName);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"✅ Sala creada: {PhotonNetwork.CurrentRoom.Name}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Falló CreateRoom. Código: {returnCode} | Mensaje: {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"❌ Desconectado de Photon. Motivo: {cause}");
        crearButton.interactable = false;
        unirButton.interactable = false;
        enLobby = false;
    }
}
