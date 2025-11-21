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
    public GameObject panelUI;
    public KeyCode teclaAbrir = KeyCode.R;
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
        if (dist <= interactionDistance && Input.GetKeyDown(teclaAbrir) && !starting)
        {
            starting = true;
            
            StateMachineStatus(MazeEnumState.Create);
        }

        
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
}
