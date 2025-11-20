using Photon.Pun;
using UnityEngine;
using System.Collections;
public class MazeStateMachine : MonoBehaviourPun
{
    public static MazeStateMachine Instance;

    public MazeEnumState currentState = MazeEnumState.Init;

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

        StateMachineStatus();
    }

    void StartMaze()
    {
        
        ChangeState(MazeEnumState.GenerateGrid);

    }


    void StateMachineStatus()
    {
        
        switch (currentState)
        {
            case MazeEnumState.GenerateGrid:
                grid.GenerateGrid();
                ChangeState(MazeEnumState.GenerateMaze);
                break;

            case MazeEnumState.GenerateMaze:
                grid.GenerateMaze();
                ChangeState(MazeEnumState.IncludeObstacles);
                break;

            case MazeEnumState.IncludeObstacles:    
                
                ChangeState(MazeEnumState.Done);
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
