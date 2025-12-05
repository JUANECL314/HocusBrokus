using System.Collections.Generic;
using UnityEngine;

public class RandomGen : MonoBehaviour
{

    List<int> lista;
    List<int> orden;
    public int[] cantidadEnemigos = { 2, 4,7  };
    public int cantidadRondas = 4;
    
    void Start()
    {
        lista = new List<int>(cantidadEnemigos);
        orden = new List<int>();
        shuffleObstacles();
        for (int i = 0; i < orden.Count; i++)
            Debug.Log(orden[i]);
    }

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
       
        lista = new List<int>(cantidadEnemigos);
        orden.Clear();
    }

}