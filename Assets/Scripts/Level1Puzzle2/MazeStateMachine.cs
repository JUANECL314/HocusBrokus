using Photon.Pun;
using System.Collections;
using UnityEngine;
public class MazeStateMachine : MonoBehaviourPun
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
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(FindLocalPlayer());
        grid = GridLayoutBase.instance;
        StateMachineStatus(currentState);
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || localPlayer == null) return;

        float dist = Vector3.Distance(localPlayer.position, transform.position);
        bool canInteract = dist <= interactionDistance;
        panelUIBoton.SetActive((canInteract && !starting));
        
        if (canInteract && Input.GetKeyDown(teclaAbrir) && !starting && currentState == MazeEnumState.Init)
        {
            starting = true;
            
            StateMachineStatus(MazeEnumState.Create);
            StartCoroutine(TimeOut());
        }
        
        
        
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
        panelMagicLeft.SetActive(true);
        panelMagicLeft.GetComponentInChildren<UnityEngine.UI.Text>().text =
            "Magia restante: " + secondsTimeOut + "s";
    }

    [PunRPC]
    void TimeoutFinishedRPC()
    {
        starting = false;
        secondsTimeOut = 15;

        panelMagicLeft.SetActive(false);
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
                }
                else
                {
                    // pedir al master que genere (opcional)
                    photonView.RPC("RPC_RequestMazeGeneration", RpcTarget.Master);
                }


                Debug.Log("Laberinto terminado");
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
