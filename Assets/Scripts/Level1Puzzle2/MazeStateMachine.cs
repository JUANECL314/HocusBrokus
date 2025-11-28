using Photon.Pun;
using System.Collections;
using UnityEngine;
using Photon.Realtime;

public class MazeStateMachine : MonoBehaviourPunCallbacks
{
    public static MazeStateMachine Instance;

    public MazeEnumState currentState = MazeEnumState.Init;

    private GridLayoutBase grid;
    private bool starting = false;
    public float interactionDistance = 2f;
    public Transform localPlayer;
    [Header("UI")]
    public GameObject panelUIBoton;
    public GameObject panelMagicLeft;

    public KeyCode teclaAbrir = KeyCode.R;
    public int secondsTimeOut = 15;
    public int contadorJugador;
    public int controlTeleport;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(FindLocalPlayer());
        grid = GridLayoutBase.instance;
        StateMachineStatus(currentState);
        if (!PhotonNetwork.IsMasterClient || localPlayer == null) return;
        starting = true;

        StateMachineStatus(MazeEnumState.Create);
    }

    void Update()
    {
        
        /*if (!PhotonNetwork.IsMasterClient || localPlayer == null) return;

        float dist = Vector3.Distance(localPlayer.position, transform.position);
        bool canInteract = dist <= interactionDistance;
        panelUIBoton.SetActive(canInteract && !starting);
        
        if (canInteract && Input.GetKeyDown(teclaAbrir) && !starting && currentState == MazeEnumState.Init)
        {
            starting = true;
            
            StateMachineStatus(MazeEnumState.Create);
            //StartCoroutine(TimeOut());
        }*/
        
        
        
    }
    IEnumerator TimeOut()
    {
        while (secondsTimeOut > 0)
        {
            yield return new WaitForSeconds(1f);

            secondsTimeOut--;
            photonView.RPC(nameof(UpdateTimeoutRPC), RpcTarget.All, secondsTimeOut);
        }

        photonView.RPC(nameof(TimeoutFinishedRPC), RpcTarget.All);
    }
    [PunRPC]
    void UpdateTimeoutRPC(int timeLeft)
    {
        secondsTimeOut = timeLeft;
        // Aquí puedes actualizar UI del contador
        //panelMagicLeft.SetActive(true);
       
    }

    [PunRPC]
    void TimeoutFinishedRPC()
    {
        starting = false;
        secondsTimeOut = 15;

        //panelMagicLeft.SetActive(false);
        StateMachineStatus(MazeEnumState.Destroy);
    }
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        // Asignar un Controller automáticamente cuando alguien se une
        AssignControllerToLastPlayer();
    }

    void AssignControllerToLastPlayer()
    {
        int lastIndex = PhotonNetwork.PlayerList.Length - 1;
        int controllerActor = PhotonNetwork.PlayerList[lastIndex].ActorNumber;

        photonView.RPC(nameof(RPC_AssignController), RpcTarget.AllBuffered, controllerActor);
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Reasignar Controller solo si era el que tenía el rol
        bool controllerStillExists = false;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            PlayerRole pr = (p.TagObject as Transform)?.GetComponent<PlayerRole>();
            if (pr != null && pr.role == TeleportRole.Controller)
            {
                controllerStillExists = true;
                break;
            }
        }

        if (!controllerStillExists)
        {
            // Asignar al último jugador de la lista como nuevo Controller
            int newControllerActor = PhotonNetwork.PlayerList[PhotonNetwork.PlayerList.Length - 1].ActorNumber;
            photonView.RPC(nameof(RPC_AssignController), RpcTarget.AllBuffered, newControllerActor);
        }
    }

    void StateMachineStatus(MazeEnumState next)
    {
        currentState = next;
        switch (currentState)
        {
            case MazeEnumState.Create:
                Debug.Log("Creando el laberinto");
                if (PhotonNetwork.IsMasterClient)
                {
                    grid.GenerateMazeMaster();
                    Invoke(nameof(StartGameplay), 0.1f);

                }
                Debug.Log("Laberinto terminado");
                break;
            case MazeEnumState.Gameplay:
                Debug.Log("Asingando");
                StartCoroutine(SetupTeleportForPlayersCoroutine());
                Debug.Log("Asignación terminada");
                break;
            case MazeEnumState.Destroy:
                Debug.Log("Destruyendo el laberinto");
                grid.DestroyMaze();
                Debug.Log("Laberinto destruido");
                currentState = MazeEnumState.Init;
                break;
            case MazeEnumState.Complete:

                break;
        }
        
    }
    void StartGameplay()
    {
        StateMachineStatus(MazeEnumState.Gameplay);
    }

    IEnumerator SetupTeleportForPlayersCoroutine()
    {
        // Esperar a que todos los jugadores tengan TagObject
        bool allReady = false;
        while (!allReady)
        {
            allReady = true;
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.TagObject == null)
                {
                    allReady = false;
                    break;
                }
            }
            yield return null;
        }

        // Agregar scripts a todos los jugadores
        foreach (var p in PhotonNetwork.PlayerList)
        {
            Transform playerTransform = p.TagObject as Transform;
            if (playerTransform == null) continue;

            if (playerTransform.GetComponent<PlayerRole>() == null)
                playerTransform.gameObject.AddComponent<PlayerRole>();

            if (playerTransform.GetComponent<TeleportController>() == null)
                playerTransform.gameObject.AddComponent<TeleportController>();
        }

        // Asignar Controller
        if (PhotonNetwork.PlayerList.Length > 0)
        {
            int controllerActor = PhotonNetwork.PlayerList[controlTeleport].ActorNumber;
            controlTeleport = (controlTeleport - 1 < 0) ? PhotonNetwork.PlayerList.Length - 1 : controlTeleport - 1;

            photonView.RPC(nameof(RPC_AssignController), RpcTarget.AllBuffered, controllerActor);
        }
    }

    IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
        {
            yield return null;
        }

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
        
    }
    
    [PunRPC]
    void RPC_RequestMazeGeneration()
    {
        if (PhotonNetwork.IsMasterClient)
            GridLayoutBase.instance.GenerateMazeMaster();
    }

    [PunRPC]
    void RPC_AssignController(int controllerActorNumber)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            Transform playerTransform = p.TagObject as Transform;
            if (playerTransform == null) continue;

            // Obtener scripts desde el Transform
            PlayerRole pr = playerTransform.GetComponent<PlayerRole>();
            TeleportController tc = playerTransform.GetComponent<TeleportController>();

            if (pr == null || tc == null) continue;

            if (p.ActorNumber == controllerActorNumber)
            {
                pr.role = TeleportRole.Controller;
                tc.canRotatePositions = true;
            }
            else
            {
                pr.role = TeleportRole.User;
                tc.canRotatePositions = false;
            }
        }
    }

}
