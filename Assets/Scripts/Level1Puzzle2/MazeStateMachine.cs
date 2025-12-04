using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class MazeStateMachine : MonoBehaviourPunCallbacks
{
    public static MazeStateMachine Instance;

    public MazeEnumState currentState = MazeEnumState.Init;
    public HiddenPuzzle hidePuzzle;
    private GridLayoutBase grid;
    public bool starting = false;
    public GameObject doorMaze;

    [Header("UI")]

    public GameObject panelMagicLeft;
    public Slider magicBar;
    public TMP_Text secondsText;
    public TMP_Text wizardsCountText;
    [Header("SettingsGame")]

    public float maxTime = 15f;
    public float secondsTimeOut = 0f;

    public int countWizards = 0;
    public int countTotalWizards = 4;

    private Coroutine timer;

    public Transform heightReference;
    public Transform respawnPoint;
    [Header("Enemy")]
    public GameObject enemy;
    public string pathEnemy = "Puzzle2Resources/";
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        grid = GridLayoutBase.instance;
        StateMachineStatus(currentState);
        doorMaze.SetActive(true);


        RestartUI();
        panelMagicLeft.SetActive(false);
        enemy.SetActive(false);
        //StateMachineStatus(MazeEnumState.Create);
    }

    void RestartUI()
    {
        if (magicBar != null)
        {
            magicBar.minValue = 0;
            magicBar.maxValue = maxTime;
            magicBar.value = secondsTimeOut;
            magicBar.interactable = false;
        }

        

        UpdateWizardsUI();
    }

    public void OnPlayersChanged()
    {
        UpdateWizardsUI();

        if (!PhotonNetwork.IsMasterClient) return;

        // Detener cualquier timer previo


        if (!starting && countWizards >= countTotalWizards)
        {


            // Subir tiempo hasta el m�ximo
            if (timer != null) StopCoroutine(timer);
            timer = StartCoroutine(timerIncrease());


        }
        else if (!starting && countWizards < countTotalWizards)
        {
            if (timer != null) StopCoroutine(timer);
            timer = StartCoroutine(timerDecrease());
        }
        if (starting && countWizards < countTotalWizards)
        {
            if (timer != null) StopCoroutine(timer);
            timer = StartCoroutine(timerDecrease());

        }
        else if (starting && countWizards == countTotalWizards)
        {
            if (timer != null) StopCoroutine(timer);
            timer = StartCoroutine(timerIncrease());
        }
        


    }


    void UpdateWizardsUI()
    {
        if (wizardsCountText != null)
            wizardsCountText.text = $"Magos: {countWizards}/{countTotalWizards}";
        if(secondsText !=null) secondsText.text = $"{secondsTimeOut.ToString()} s";

    }
    IEnumerator timerIncrease()
    {
        while (secondsTimeOut < maxTime)
        {
            yield return new WaitForSeconds(1f);
            secondsTimeOut++;
            photonView.RPC(nameof(UpdateTimeoutRPC), RpcTarget.All, secondsTimeOut);
            if (!starting && secondsTimeOut == maxTime && currentState == MazeEnumState.Init)
            {
                StateMachineStatus(MazeEnumState.Create);
            }
        }
    }
    IEnumerator timerDecrease()
    {
        while (secondsTimeOut > 0 && countWizards < countTotalWizards)
        {
            yield return new WaitForSeconds(1f);
            secondsTimeOut--;
            photonView.RPC(nameof(UpdateTimeoutRPC), RpcTarget.All, secondsTimeOut);
        }

        if (secondsTimeOut <= 0)
            photonView.RPC(nameof(TimeoutFinishedRPC), RpcTarget.All);
    }



    [PunRPC]
    void UpdateTimeoutRPC(float timeLeft)
    {
        secondsTimeOut = timeLeft;
        if (secondsText != null) secondsText.text = $"{secondsTimeOut.ToString()} s";
        float valueSecond = Mathf.Clamp(secondsTimeOut, 0, maxTime);
        if (magicBar != null)
        {
            magicBar.value = valueSecond;
        }


    }

    [PunRPC]
    void TimeoutFinishedRPC()
    {
        starting = false;
        secondsTimeOut = 0f;
        if (secondsText != null) secondsText.text = $"{secondsTimeOut.ToString()} s";
        float valueSecond = Mathf.Clamp(secondsTimeOut, 0, maxTime);
        if (magicBar != null)
        {
            magicBar.value = valueSecond;
        }
        
        Respawn();
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
                    starting = true;
                    grid.GenerateMazeMaster();
                    Invoke(nameof(StartGameplay), 0.1f);
                    ObtainPuzzleStructure();
                }
                Debug.Log("Laberinto terminado");
                break;
            case MazeEnumState.Gameplay:
                Debug.Log("Empezar configuraciones de partida");

                doorMaze.SetActive(false);
                
                
                if (PhotonNetwork.IsMasterClient)
                {
                    //PhotonNetwork.Instantiate(pathEnemy + enemy.name, enemy.transform.position, enemy.transform.rotation);
                }
                Debug.Log("Configuraciones terminadas");
                break;
            case MazeEnumState.Destroy:
                Debug.Log("Destruyendo el laberinto");

                grid.DestroyMaze();
                doorMaze.SetActive(true);
                RestartUI();
                Debug.Log("Laberinto destruido");
                currentState = MazeEnumState.Init;
                break;
            case MazeEnumState.Complete:
                Debug.Log("Laberinto completado");
                if(timer != null) StopCoroutine(timer);
                grid.DestroyMaze();
                doorMaze.SetActive(false);
                enemy.SetActive(false);
                starting = false;
                break;
        }

    }
    void StartGameplay()
    {
        StateMachineStatus(MazeEnumState.Gameplay);
    }
    public void CompleteGame() {
        StateMachineStatus(MazeEnumState.Complete);
    }

    [PunRPC]
    void RPC_RequestMazeGeneration()
    {
        if (PhotonNetwork.IsMasterClient)
            GridLayoutBase.instance.GenerateMazeMaster();
    }

    [PunRPC]
    public void UpdateUIRPC(bool show)
    {


        if (panelMagicLeft != null)
            panelMagicLeft.SetActive(show);
    }
    [PunRPC]
    public void UpdateWizardsUIRPC(int wizards)
    {
        countWizards = wizards;
        UpdateWizardsUI();
    }


    void Respawn()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.TagObject == null) continue;

            Transform playerTransform = player.TagObject as Transform;
            if (playerTransform == null) continue;

            // Solo si está debajo de la referencia de altura
            if (playerTransform.position.y < heightReference.position.y)
            {
                PhotonView playerPV = playerTransform.GetComponent<PhotonView>();
                if (playerPV != null)
                {
                    // Llamamos un RPC que solo se ejecuta en la máquina del dueño
                    playerPV.RPC("RPC_TeleportPlayer", playerPV.Owner, respawnPoint.position, respawnPoint.rotation);
                }
            }
        }
    }
    [PunRPC]
    public void RPC_TeleportPlayer(Vector3 pos, Quaternion rot)
    {
        // Solo mover si este cliente es el dueño del objeto
        if (!photonView.IsMine) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(pos);
            rb.MoveRotation(rot);
        }
        else
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }

    void ObtainPuzzleStructure()
    {
        if (hidePuzzle.padre == null) return;
        hidePuzzle.meshRenderers = hidePuzzle.padre.GetComponentsInChildren<MeshRenderer>();

    }
}
