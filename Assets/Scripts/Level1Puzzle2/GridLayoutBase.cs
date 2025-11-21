using System.Collections.Generic;
using UnityEngine;

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
    public Vector2Int goalNode;
    public Vector2Int startNode = new Vector2Int(0, 1);

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
        GenerateGrid();
        GenerateMaze();
        Carve(startNode.y, startNode.x, 0);
        MarkGoalNode();
    }

    public void ReplaceTile(int y, int x, GameObject prefab)
    {
        GameObject old = tiles[y][x];

        GameObject newTile = Instantiate(
            prefab,
            old.transform.position,
            Quaternion.identity,
            transform);

        tiles[y][x] = newTile;
        Destroy(old);
    }
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        tiles.Clear();
        visited = new bool[rows, columns];

        for (int y = 0; y < rows; y++)
        {
            tiles.Add(new List<GameObject>());

            for (int x = 0; x < columns; x++)
            {
                Vector3 pos = new Vector3(
                    transform.position.x + (x * tileSize),
                    0,
                    transform.position.z + (y * tileSize));

                GameObject tile = Instantiate(wallPrefab, pos, Quaternion.identity);
                tile.transform.parent = transform;
                tiles[y].Add(tile);
            }
        }
    }

    public void GenerateMaze()
    {
        visited = new bool[rows, columns];

    }

    // ---- DFS ----
    private void Carve(int y, int x, int distance)
    {
        visited[y, x] = true;

        // Determinar nodo más lejano
        if (distance > longestDistance)
        {
            longestDistance = distance;
            goalNode = new Vector2Int(x, y);
        }

        // Mezclar direcciones
        List<Vector2Int> direccion = new List<Vector2Int>(directions);
        Shuffle(direccion);

        foreach (Vector2Int dir in direccion)
        {
            int ny = y + dir.y;
            int nx = x + dir.x;

            if (Inside(ny, nx) && !visited[ny, nx])
            {
                ReplaceTile(y, x, floorPrefab);
                ReplaceTile(ny, nx, floorPrefab);

                Carve(ny, nx, distance + 1);
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
        return y >= 0 && y < rows && x >= 0 && x < columns;
    }

    // ---- MARCAR NODO FINAL (GOAL) ----
    private void MarkGoalNode()
    {
        Debug.Log("Nodo final del laberinto (goal): " + goalNode);

        if (goalPrefab == null) return;

        Vector3 pos = tiles[goalNode.y][goalNode.x].transform.position;

        Instantiate(goalPrefab, pos, Quaternion.identity, transform);
    }
}
