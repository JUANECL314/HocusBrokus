using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Photon.Pun;
using UnityEngine;


public class GridLayoutBase : MonoBehaviourPun
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

    public int[,] mazeData;

    public GameObject[] obstacles;
    public GameObject teletransportEnemy;
    [Header("Obstacles Settings")]
    public float obstacleProbability = 0.15f;
    public int minObstacles = 4;

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
        mazeData = new int[rows, columns];
    }

    void Start()
    {
        DestroyMaze();

    }

    public void ReplaceTile(int y, int x, GameObject prefab)
    {
        GameObject old = tiles[y][x];
        int type = 0;
        if (prefab == floorPrefab) type = 1;
        else if (prefab == goalPrefab) type = 2;
        if (mazeData == null)
            mazeData = new int[rows, columns];
        mazeData[y, x] = type;
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
    public void DestroyMaze()
    {
        tiles = new List<List<GameObject>>();
        List<GameObject> toDelete = new List<GameObject>();
        foreach (Transform child in transform)
        {
            toDelete.Add(child.gameObject);
        }
        foreach (GameObject obj in toDelete)
        {

            if (UnityEngine.Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
        tiles.Clear();
    }
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        DestroyMaze();
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

        SpawnObstacles();
        // Marcar goal (luego de carve)
        MarkGoalNode();
    }
    private void SpawnObstacles()
    {
        if (obstacles == null || obstacles.Length == 0) return;
        List<Vector2Int> floorTiles = new List<Vector2Int>();
        // Obtener todas las celdas walkable
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (mazeData[y, x] == 1) // 1 = floor
                {
                    // Evitar start y goal
                    if (x == startNode.x && y == startNode.y) continue;
                    if (x == goalNode.x && y == goalNode.y) continue;

                    floorTiles.Add(new Vector2Int(x, y));
                }
            }
        }
        int placed = 0;
        foreach (var tile in floorTiles)
        {
            if (UnityEngine.Random.value <= obstacleProbability)
            {
                PlaceObstacle(tile.y, tile.x);
                placed++;
            }
        }
        while (placed < minObstacles && floorTiles.Count > 0)
        {
            int i = UnityEngine.Random.Range(0, floorTiles.Count);
            var tile = floorTiles[i];

            PlaceObstacle(tile.y, tile.x);
            floorTiles.RemoveAt(i);
            placed++;
        }
    }
    private void PlaceObstacle(int y, int x)
    {
        GameObject tile = tiles[y][x];
        if (tile == null || tile.name == "Wall(Clone)") return;

        // Seleccionar obstáculo aleatorio
        GameObject prefab = obstacles[UnityEngine.Random.Range(0, obstacles.Length)];

        Vector3 pos = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.2f, tile.transform.position.z);
        GameObject obs;
        if (PhotonNetwork.IsConnected)
        {
            obs = PhotonNetwork.Instantiate("ObstaclePrefabs/" + prefab.name, pos, Quaternion.identity);
        } 
        else
        {
            obs = Instantiate(prefab, pos, Quaternion.identity);
        }
            

        obs.transform.parent = transform;
        string name = prefab.name;
        Vector3 escala;
        switch(name)
        {
            case "VortexObstacle":
                escala = new Vector3(0.7897533f, 0.5265021f, 0.3948766f);
                //obs.GetComponent<VortexObstacle>().
                break;
            case "FieryObstacle":
                escala = new Vector3(0.2151325f, 0.2151325f, 0.2151325f);
                //obs.GetComponent<VortexObstacle>().
                break;
            case "RockObstacle":
                escala = new Vector3(1f, 5.80744f, 1f);
                float resto = 9.87f - tile.transform.position.y;
                pos = new Vector3(tile.transform.position.x, tile.transform.position.y + resto, tile.transform.position.z-2);
                obs.transform.position = pos;
                //obs.GetComponent<VortexObstacle>().
                break;
            case "TreeObstacle":
                escala = new Vector3(1f, 2.9f, 1f);
                //obs.GetComponent<VortexObstacle>().
                break;
            default:
                escala = new Vector3(1f, 1f, 1f);
                break;

        }
        obs.transform.localScale = escala;
        UnityEngine.Debug.Log($"Obstacle {name} placed at ({x},{y}) with scale {escala}");
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

                // Recursivo
                CarveRecursive(ny, nx, distance + 1);
            }
        }
    }
   
   
    public static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
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
    public void ApplyMazeData(int[,] data)
    {
        mazeData = data;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int type = mazeData[y, x];

                if (type == 0)
                    ReplaceTile(y, x, wallPrefab);
                else if (type == 1)
                    ReplaceTile(y, x, floorPrefab);
                else if (type == 2)
                    ReplaceTile(y, x, goalPrefab);
            }
        }
    }

}
