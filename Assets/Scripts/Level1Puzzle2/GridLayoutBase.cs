using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class GridLayoutBase : MonoBehaviour
{
    public static GridLayoutBase instance;

    [Header("Prefabs")]
    
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject goalPrefab; 
    

    [Header("Grid Dimensions")]
    public int rows;
    public int columns;
    public int tileSize = 4;

    public List<List<GameObject>> tiles = new List<List<GameObject>>();
    public bool[,] visited;

    // Para detectar el nodo final
    private int longestDistance = 0;
    public Vector2Int goalNode = new Vector2Int(14,20);
    public Vector2Int startNode = new Vector2Int(1,0);

    private static readonly List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        tiles = new List<List<GameObject>>();
        
        
    }

    public void ReplaceTile(int y, int x, GameObject prefab)
    {
        GameObject old = tiles[y][x];

        float altura= (prefab.tag == "Floor" || prefab.tag == "Goal") ? 1f : prefab.transform.position.y;
        Vector3 pos = new Vector3(
            old.transform.position.x,
            altura,
            old.transform.position.z);

        GameObject newTile = Instantiate(
            prefab,
            pos,
            Quaternion.identity,
            transform);

        tiles[y][x] = newTile;
        if (UnityEngine.Application.isPlaying)
            Destroy(old);
        else
            DestroyImmediate(old);
    }
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {

        tiles = new List<List<GameObject>>();
        

        
        List<GameObject> toDelete = new List<GameObject>();
        foreach (Transform child in transform)
        {
            toDelete.Add(child.gameObject);
        }
            

        
        foreach (GameObject obj in toDelete)
        {
            
           if(UnityEngine.Application.isPlaying)
                Destroy(obj);
           else
                DestroyImmediate(obj);
        }
        visited = new bool[rows, columns];

        for (int y = 0; y < rows; y++)
        {
            tiles.Add(new List<GameObject>());

            for (int x = 0; x < columns; x++)
            {
                Vector3 pos = new Vector3(
                    transform.position.x + (x * tileSize),
                    wallPrefab.transform.position.y,
                    transform.position.z + (y * tileSize));
                GameObject tile = Instantiate(wallPrefab, pos, Quaternion.identity);
                tile.transform.parent = transform;
                tiles[y].Add(tile);
            }
        }
        tiles[startNode.y][startNode.x].SetActive(false);
        tiles[goalNode.y][goalNode.x].SetActive(false);
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        
        // Validaciones
        if (rows < 3 || columns < 3)
        {
            
            return;
        }

        if (rows % 2 == 0 || columns % 2 == 0)
        {
            return;
        }

        visited = new bool[rows, columns];
        
        longestDistance = 0;
        

        // Asegúrate de que las tiles ya existen (GenerateGrid) y que startNode sea válido
        if (tiles == null || tiles.Count == 0) GenerateGrid();

        // Opcional: asegurarse de que startNode está dentro
        startNode.x = Mathf.Clamp(startNode.x, 0, columns - 1);
        startNode.y = Mathf.Clamp(startNode.y, 0, rows - 1);

        // Empezar DFS (asegúrate de que el punto inicial también sea un 'floor')
        
        ReplaceTile(startNode.y, startNode.x, floorPrefab);

        CarveRecursive(startNode.y, startNode.x, 0);

        // Marcar goal (luego de carve)
        MarkGoalNode();
    }
    private void CarveRecursive(int y, int x, int distance)
    {
        visited[y, x] = true;

        // Actualizar nodo más lejano
        if (distance > longestDistance)
        {
            longestDistance = distance;
            
        }

        // Mezclar direcciones
        List<Vector2Int> dirs = new List<Vector2Int>(directions);
        Shuffle(dirs);

        foreach (Vector2Int dir in dirs)
        {
            int ny = y + dir.y * 2;
            int nx = x + dir.x * 2;

            int betweenY = y + dir.y;
            int betweenX = x + dir.x;

            if (Inside(ny, nx) && !visited[ny, nx])
            {
                
                // Abrir el muro entre celdas
                ReplaceTile(betweenY, betweenX, floorPrefab);

                // Abrir la celda destino
                ReplaceTile(ny, nx, floorPrefab);

                // Recurse
                CarveRecursive(ny, nx, distance + 1);
            }
        }
    }
   
   
    public static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    private bool Inside(int y, int x)
    {
        return y > 0 && y < rows-1 && x > 0 && x < columns-1;
    }

    
    private void MarkGoalNode()
    {
        

        if (goalPrefab == null) return;

        Vector3 pos = new Vector3 (tiles[goalNode.y][goalNode.x].transform.position.x,1, tiles[goalNode.y][goalNode.x].transform.position.z);

        GameObject goalObj = Instantiate(goalPrefab, pos, Quaternion.identity, transform);
        goalObj.tag = "Goal";
    }
}
