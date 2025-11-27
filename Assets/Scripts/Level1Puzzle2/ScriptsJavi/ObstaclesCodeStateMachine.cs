using Photon.Pun;
using System;
using System.Collections.Generic;

using UnityEngine;

public class ObstaclesCodeStateMachine : MonoBehaviourPun
{
    public static ObstaclesCodeStateMachine instance;

    [Header("Obstacles")]
    public GameObject[] obstacles;   // Prefabs registrados en Photon
    public string pathObstacles = "Elements/";
    [Header("ID_Obstacles (Water=0, Fire=1, Earth=2, Wind=3)")]
    public int[] id = { 0, 1, 2, 3 };
    List<int> lista;
    List<int> orden;

    [Header("Cantidad Obstáculos")]
    int limiteDeObstaculos = 5;
    public int totalCantidadAgua;
    public int totalCantidadFuego;
    public int totalCantidadTierra;
    public int totalCantidadAire;

    [Header("Posiciones")]
    public Transform[] posiciones;

    public enum ObstaclesState
    {
        Init,
        Create,
        Play,
        Complete
    }

    public ObstaclesState currentState = ObstaclesState.Init;

    private bool _generarObstaculosConteo = false;
    private bool _empezarJuego = false;

    [Header("Pilares")]
    public GameObject[] estatuas;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        lista = new List<int>(id);
        orden = new List<int>();

        _generarObstaculosConteo = true;
    }

    void Update()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        // SOLO MASTER EJECUTA LA LÓGICA
        if (!PhotonNetwork.IsMasterClient) return;

        if (_generarObstaculosConteo && !_empezarJuego)
            StateMachineStatus(ObstaclesState.Create);

        else if (_empezarJuego && !_generarObstaculosConteo)
            StateMachineStatus(ObstaclesState.Play);
    }

    // ==============================
    //  MAQUINA DE ESTADOS
    // ==============================

    void StateMachineStatus(ObstaclesState next)
    {
        currentState = next;

        switch (currentState)
        {
            case ObstaclesState.Create:
                Debug.Log("----- CREANDO OBSTÁCULOS -----");

                _generarObstaculosConteo = false;

                ResetLista();
                shuffleObstacles();
                cantObstaculos();
                aparecerObstaculos();
                photonView.RPC("RPC_UpdateTotals", RpcTarget.AllBuffered);

                _empezarJuego = true;

                Debug.Log("----- FIN CREATE -----");
                break;

            case ObstaclesState.Play:
                _empezarJuego = false;
                Debug.Log("----- EMPEZAR JUEGO -----");
                break;
        }
    }

    // ==============================
    //  GENERAR ORDEN
    // ==============================

    void shuffleObstacles()
    {
        while (lista.Count > 0)
        {
            orden.Add(ShuffleOrderCode());
        }
    }

    int ShuffleOrderCode()
    {
        int index = UnityEngine.Random.Range(0, lista.Count);
        int value = lista[index];
        lista.RemoveAt(index);
        return value;
    }

    void ResetLista()
    {
        limiteDeObstaculos = 5;
        lista = new List<int>(id);
        orden.Clear();
    }

    // ==============================
    //  CANTIDADES
    // ==============================

    void cantObstaculos()
    {
        limiteDeObstaculos++;

        totalCantidadAgua = UnityEngine.Random.Range(1, limiteDeObstaculos);
        totalCantidadFuego = UnityEngine.Random.Range(1, limiteDeObstaculos);
        totalCantidadTierra = UnityEngine.Random.Range(1, limiteDeObstaculos);
        totalCantidadAire = UnityEngine.Random.Range(1, limiteDeObstaculos);

        photonView.RPC(
           "RPC_SetTotals",
           RpcTarget.OthersBuffered,
           totalCantidadAgua,
           totalCantidadFuego,
           totalCantidadTierra,
           totalCantidadAire
        );
    }

    int seleccionarTotal(int id)
    {
        switch (id)
        {
            case 0: return totalCantidadAgua;
            case 1: return totalCantidadFuego;
            case 2: return totalCantidadTierra;
            case 3: return totalCantidadAire;
        }
        return 0;
    }

    // ==============================
    //  GENERAR OBSTÁCULOS
    // ==============================

    void aparecerObstaculos()
    {
        for (int i = 0; i < posiciones.Length; i++)
        {
            int ordenActual = orden[i];
            int total = seleccionarTotal(ordenActual);
            Transform pos = posiciones[i];

            for (int j = 0; j < total; j++)
            {
                Vector3 newPos = pos.position;
                newPos.z += j * 3f;

                // AHORA SÍ ES CORRECTO ✨
                PhotonNetwork.Instantiate(pathObstacles+obstacles[ordenActual].name, newPos, Quaternion.identity);
            }
        }
    }

    // ==============================
    //  ESTATUAS
    // ==============================

    void ConectarEstatuas()
    {
        foreach (GameObject estatua in estatuas)
        {
            CounterCode contador = estatua.GetComponent<CounterCode>();
            string tagEstatua = contador.tagName;

            contador.total = seleccionarTag(tagEstatua);
        }
    }

    int seleccionarTag(string tag)
    {
        switch (tag)
        {
            case "Water": return totalCantidadAgua;
            case "Fire": return totalCantidadFuego;
            case "Earth": return totalCantidadTierra;
            case "Wind": return totalCantidadAire;
        }
        return 0;
    }

    // ==============================
    //  RPCs
    // ==============================

    [PunRPC]
    void RPC_SetTotals(int agua, int fuego, int tierra, int aire)
    {
        totalCantidadAgua = agua;
        totalCantidadFuego = fuego;
        totalCantidadTierra = tierra;
        totalCantidadAire = aire;
    }

    [PunRPC]
    void RPC_UpdateTotals()
    {
        ConectarEstatuas();
    }
}
