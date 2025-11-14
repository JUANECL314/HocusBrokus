using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance; // Singleton

    [Header("Escenas")]
    public string pantallaInicio = "MainMenu"; // Pantalla Inicial
    public string salaIndividualNombre = "Lobby"; // Escena de sala individual
    public string salaMultijugadorNombre = "TownRoom"; // Escena de sala multijugador

    private bool entroPorBoton = false; // Bandera de control para el ingreso de escenas
    public int maxPlayers = 4;
    private string nombreSalaParaCrear = null; 
    
    void Awake()
    {
        // Evitar duplicados
        if ( Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Instanciar el singleton
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
        ConectarServidor();

    }
    // --------------------------- Conexión a Photon ----------------------
     
    public void ConectarServidor()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Conexión a photon exitoso.");
            return;
        }

        Debug.Log("Realizando conexión a photon...");
        // Se asegura que no este en offline
        //PhotonNetwork.OfflineMode = false;
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "us"; 
        

        PhotonNetwork.ConnectUsingSettings();
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectando al servidor principal de Photon");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Unido al lobby de Photon");
        var roomListManager = FindObjectOfType<RoomListManager>();
        if (roomListManager != null)
        {
            roomListManager.RefreshList();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Desconectado de Photon: " + cause);
    }
    // --------------------- Lobby individual ------------------------
    public void EntrarLobbyIndividual()
    {     
        Debug.Log($"Cargando escena del lobby: {salaIndividualNombre}");
        CargarEscenarioLocal(salaIndividualNombre);
    }

    // ----------------------- Crear/Unir salas multijugador -----------
    public void CrearSala(string nombreSala)
    {
        if (!PhotonNetwork.IsConnected) {
            Debug.LogWarning("No conectado a photon. Es necesario la conexión");
            return;
        }

     
        
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,    
            IsOpen = true
        };

        
        PhotonNetwork.CreateRoom(nombreSala, options);
        
    }

    public void UnirSala(string nombreSala)
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("No conectado a photon. Es necesario la conexión");
            return;
        }
        if (PhotonNetwork.InRoom)
        {
            nombreSalaParaCrear = nombreSala;
            PhotonNetwork.LeaveRoom(); // Deja la sala actual antes de crear otra
            return; // Espera al callback OnLeftRoom
        }
        entroPorBoton = false;
        PhotonNetwork.JoinRoom(nombreSala);
        
    }

    public override void OnLeftRoom()
    {
    }

    public override void OnJoinedRoom()
    {
        string nombreSala = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "Sin sala";
        Debug.Log("Entro a la sala: " + nombreSala);

        // Buscar el UI de espera dentro de la escena actual
        WaitingRoomUI waitingUI = FindObjectOfType<WaitingRoomUI>();

        if (waitingUI != null)
        {
            waitingUI.gameObject.SetActive(true);
            waitingUI.SendMessage("ActualizarUI", SendMessageOptions.DontRequireReceiver);
        }

    }


   public void CargarEscenario(string nombreEscena)
    {
        
        if(PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.LoadLevel(nombreEscena);
        }
        else
        {
            StartCoroutine(ReintentarConexion(nombreEscena));
        }
    }

    private IEnumerator ReintentarConexion(string nombreEscena)
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        float tiempoMaximo = 10f;
        float tiempo = 0f;

        while (!PhotonNetwork.IsConnectedAndReady && tiempo < tiempoMaximo)
        {
            tiempo += Time.deltaTime;
            yield return null;
        }
        CargarEscenario(nombreEscena);
    }
    public void CargarEscenarioLocal(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }

    // ------------------- Manejo cuando el MasterClient se desconecta -------------------

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"El MasterClient se ha desconectado. Nuevo Master: {newMasterClient.NickName}");

        // Puedes elegir entre las dos opciones según el comportamiento que desees:
        // OPCIÓN 1: todos regresan al lobby manualmente (más segura)
        StartCoroutine(RegresarAlLobby());

        
    }

    private IEnumerator RegresarAlLobby()
    {
        Debug.Log("Saliendo de la sala y regresando al lobby...");

        PhotonNetwork.LeaveRoom();

        // Espera hasta salir de la sala
        while (PhotonNetwork.InRoom)
            yield return null;

        CargarEscenarioLocal("Lobby");
    }

    void OnApplicationQuit()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Jugador cerró el juego, saliendo de la sala y desconectando de Photon...");
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
        }
    }


    public void MasterIniciarPartida()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(IniciarPartida), RpcTarget.All);
        }
    }
    [PunRPC]
    public void IniciarPartida()
    {
        PhotonNetwork.LoadLevel(salaMultijugadorNombre);
    }
}
