using Photon.Pun;
using UnityEngine;
using System.Collections;
public class MazeStateMachine : MonoBehaviourPun
{
    public static MazeStateMachine Instance;

    public MazeEnumState currentState = MazeEnumState.Create;

    private GridLayoutBase grid;
    private bool starting = false;
    public float interactionDistance = 2f;
    public Transform localPlayer;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(FindLocalPlayer());
        grid = GridLayoutBase.instance;
        StateMachineStatus();
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || localPlayer == null) return;

        float dist = Vector3.Distance(localPlayer.position, transform.position);

        if (dist <= interactionDistance && Input.GetKeyDown(KeyCode.R) && !starting)
        {
            starting = true;
            StartMaze();
        }

        
    }

    void StartMaze()
    {
        
        ChangeState(MazeEnumState.Create);

    }


    void StateMachineStatus()
    {
        
        switch (currentState)
        {
            case MazeEnumState.Create:
                Debug.Log("Creando el laberinto");
                grid.GenerateGrid();
                grid.GenerateMaze();
                Debug.Log("Laberinto terminado");
                break;
            case MazeEnumState.Complete:

                break;
        }
        
    }


    public void ChangeState(MazeEnumState state)
    {
        currentState = state;
    }

    IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
        {
            yield return null;
        }

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
        
    }
}
