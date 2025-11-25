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
    public GameObject panelUI;
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
        if (dist <= interactionDistance && !starting)
        {   
            
            if (!panelUI.activeSelf)
                panelUI.SetActive(true);
        }
        else
        {
            if (panelUI.activeSelf)
                panelUI.SetActive(false);
        }
        if (dist <= interactionDistance && Input.GetKeyDown(teclaAbrir) && !starting && currentState == MazeEnumState.Init)
        {
            starting = true;
            
            StateMachineStatus(MazeEnumState.Create);
            StartCoroutine(TimeOut());
        }
        
        
        
    }
    IEnumerator TimeOut()
    {
        // Esperar un tiempo determinado - 1 seg
        yield return new WaitForSeconds(1f);
        
        secondsTimeOut -= 1;
        if (secondsTimeOut == 0)
        {
            starting = false;
            secondsTimeOut = 15;
            StateMachineStatus(MazeEnumState.Destroy);
            yield break;
        }
        StartCoroutine(TimeOut());

    }
   

    void StateMachineStatus(MazeEnumState next)
    {
        currentState = next;
        switch (currentState)
        {
            case MazeEnumState.Create:
                Debug.Log("Creando el laberinto");
                grid.GenerateGrid();
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
    void RPC_SendMaze(int[,] data)
    {
        StartCoroutine(ApplyMazeWhenReady(data));
    }

    IEnumerator ApplyMazeWhenReady(int[,] data)
    {
        // Esperar a que grid exista
        while (GridLayoutBase.instance == null)
            yield return null;

        grid = GridLayoutBase.instance;
        grid.ApplyMazeData(data);
    }


}
