using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
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
        PhotonNetwork.OfflineMode = false;
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

        if (PhotonNetwork.InRoom)
        {

            nombreSalaParaCrear = nombreSala;
            PhotonNetwork.LeaveRoom(); 
            return; 
        }
        entroPorBoton = true;
        PhotonNetwork.CreateRoom(nombreSala);
        
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
        entroPorBoton = true;
        PhotonNetwork.JoinRoom(nombreSala);
        
    }

    public override void OnLeftRoom()
    {
        if (!string.IsNullOrEmpty(nombreSalaParaCrear))
        {
            PhotonNetwork.CreateRoom(nombreSalaParaCrear);
            nombreSalaParaCrear = null;
        }
    }

    public override void OnJoinedRoom()
    {
        if (entroPorBoton)
        {
            CargarEscenario(salaMultijugadorNombre);
            entroPorBoton =false;
        }
        
    }


   public void CargarEscenario(string nombreEscena)
    {
        if(PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.LoadLevel(nombreEscena);
        }
    }

    public void CargarEscenarioLocal(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }
}
