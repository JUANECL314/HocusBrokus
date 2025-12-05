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

        if (secondsTimeOut <= 0 && starting)
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
        
        
        StateMachineStatus(MazeEnumState.Destroy);
    }

    [PunRPC]
    void RPC_SetObjectActive(bool state)
    {
        doorMaze.SetActive(state);
    }

    public void SetActiveMultiplayer(bool state)
    {
        photonView.RPC("RPC_SetObjectActive", RpcTarget.AllBuffered, state);
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
                    photonView.RPC("RPC_HideStructure", RpcTarget.AllBuffered);
                    
                }
                Debug.Log("Laberinto terminado");
                break;
            case MazeEnumState.Gameplay:
                Debug.Log("Empezar configuraciones de partida");

                SetActiveMultiplayer(false);
                   
                
                Debug.Log("Configuraciones terminadas");
                break;
            case MazeEnumState.Destroy:
                Debug.Log("Destruyendo el laberinto");
        
                grid.DestroyMaze();
                SetActiveMultiplayer(true);
                RestartUI();
                Debug.Log("Laberinto destruido");
                currentState = MazeEnumState.Init;
                break;
            case MazeEnumState.Complete:
                Debug.Log("Laberinto completado");
                if(timer != null) StopCoroutine(timer);
                grid.DestroyMaze();
                SetActiveMultiplayer(true);

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


    
    

    [PunRPC]
    public void RPC_HideStructure()
    {
        ObtainPuzzleStructure();
    }
    void ObtainPuzzleStructure()
    {
        if (hidePuzzle.padre == null) return;
        hidePuzzle.meshRenderers = hidePuzzle.padre.GetComponentsInChildren<MeshRenderer>();

    }
}
