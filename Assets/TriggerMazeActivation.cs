using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;


public class TriggerMazeActivation : MonoBehaviour
{
    public static TriggerMazeActivation Instance;

    public MazeStateMachine mazeMachine;

    public string playerTag = "Player";

    public int requiredPlayers = 4;

// public HashSet<Trasform> playersInside = new HashSet<Trasform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if(mazeMachine == null)
        {
            //mazeMachine = mazeMachine.currentState;
        }
    }


}
