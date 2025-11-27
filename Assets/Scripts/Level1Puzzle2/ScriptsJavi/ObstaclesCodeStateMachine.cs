using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
public class ObstaclesCodeStateMachine : MonoBehaviourPun
{
    public static ObstaclesCodeStateMachine instance;
    [Header("Obstacles")]
    public GameObject[] obstacles;

    [Header("ID_Obstacles")]
    // Water - 0, Fire - 1, Earth - 2, Wind - 3
    public int[] id = { 0, 1, 2, 3 };
    List<int> lista;
    List<int> orden;

    [Header("Cantidad_Obstáculos")]
    int limiteDeObstaculos = 5;
    public int totalCantidadAgua;
    public int totalCantidadFuego;
    public int totalCantidadTierra;
    public int totalCantidadAire;
    
    [Header("Posiciones_Obstaculos")]
    public Transform[] posiciones;

    [Header("EstadoActual")]
    public ObstaclesState currentState = ObstaclesState.Init;
    public enum ObstaclesState
    {
        Init,
        Create,
        Play,
        Destroy,
        Complete
    }

    [Header("Condiciones")]
    private bool _generarObstaculosConteo = false;
    private bool _empezarJuego = false;
    [Header("Pilares")]
    // Water - 0, Fire - 1, Earth - 2, Wind - 3
    public GameObject[] estatuas;
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); //  destruye el objeto duplicado
            return;
        }

        instance = this;
        lista = new List<int>(id);
        orden = new List<int>();
        _generarObstaculosConteo = true;
    }
    
   
    void Update()
    {
        if (!PhotonNetwork.IsMasterClient && !PhotonNetwork.IsConnectedAndReady) return;
        if (_generarObstaculosConteo)
        {
            StateMachineStatus(ObstaclesState.Create);
        }
        else if(_empezarJuego)
        {
            StateMachineStatus(ObstaclesState.Play);
        }
        
    }

    void shuffleObstacles()
    {
        while (lista.Count > 0)
        {
            orden.Add(ShuffleOrderCode());
        }


        _generarObstaculosConteo = false;
    }

    void cantObstaculos()
    {
        limiteDeObstaculos += 1;
        totalCantidadAgua = randomCant(1,limiteDeObstaculos);
        totalCantidadFuego = randomCant(1,limiteDeObstaculos);
        totalCantidadTierra = randomCant(1,limiteDeObstaculos);
        totalCantidadAire = randomCant(1,limiteDeObstaculos);
        Debug.Log("Total agua" + totalCantidadAgua);
        Debug.Log("Total fuego" + totalCantidadFuego);
        Debug.Log("Total tierra" + totalCantidadTierra);
        Debug.Log("Total aire" + totalCantidadAire);
    }

    int randomCant(int inicio,int limite)
    {
        return UnityEngine.Random.Range(inicio, limite);
    }
    int ShuffleOrderCode()
    {
        int randomPick = lista[randomCant(0,lista.Count)];
        lista.Remove(randomPick);
        return randomPick;
    }
    void aparecerObstaculos()
    {
        int contador = 0;
        while (contador < posiciones.Length) {
            int ordenActual = orden[contador];
            GameObject obstaculoActual = obstacles[ordenActual];
            int total = seleccionarTotal(ordenActual);
            Transform posicionActual = posiciones[contador];
            
            for (int i = 0; i < total; i++) {
                GameObject nuevo;
                if (PhotonNetwork.IsConnectedAndReady)
                {
                    nuevo = PhotonNetwork.Instantiate(obstaculoActual.name);
                }
                else
                {
                    nuevo = Instantiate(obstaculoActual);
                }
                    
                Vector3 pos = posicionActual.position;
                pos.z += i * 3f;
                nuevo.transform.position = pos; 
            }
            contador += 1;
        }
    }

    void ConectarEstatus()
    {
        foreach(GameObject estatua in estatuas)
        {
            CounterCode contador = estatua.GetComponent<CounterCode>();
            string tagEstatua = contador.tagName;
            
            contador.total = seleccionarTag(tagEstatua);
        }
    }
    int seleccionarTotal(int id)
    {
        switch (id)
        {
            case 0:
                return totalCantidadAgua;
            case 1:
                return totalCantidadFuego;
            case 2:
                return totalCantidadTierra;
            case 3:
                return totalCantidadAire;
            default:
                return 0;
        }
    }

    int seleccionarTag(string id)
    {
        switch (id)
        {
            case "Water":
                return totalCantidadAgua;
            case "Fire":
                return totalCantidadFuego;
            case "Earth":
                return totalCantidadTierra;
            case "Wind":
                return totalCantidadAire;
            default:
                return 0;
        }
    }

void StateMachineStatus(ObstaclesState next)
    {
        currentState = next;
        switch (currentState)
        {
            case ObstaclesState.Create:
                Debug.Log("------------------------------------------- Creando código");
                
                _generarObstaculosConteo = false;
                shuffleObstacles();
                cantObstaculos();
                aparecerObstaculos();
                ConectarEstatus();
                _empezarJuego = true;
                Debug.Log("------------------------------------------- Terminando código");
                break;
            case ObstaclesState.Play:
                _empezarJuego = false;
                Debug.Log("------------------------------------------- Empezar juego");
                
                Debug.Log("------------------------------------------- Terminar juego");
                break;
            
        }

    }

   
}
