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

        StateMachineStatus(MazeEnumState.Init);
    }

    void Update()
    {
        
       
        
        
        
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


}
