using System.Collections.Generic;
using System.Drawing;
using UnityEditor.Callbacks;
using UnityEngine;

public class GridLayoutBase : MonoBehaviour
{
    public static GridLayoutBase instance;
    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
   
    [Header("Grid Dimensions")]
    public int rows;
    public int columns;
    private float spacing = 0f;

    [Header("Tile dimension")]
    public int tileSize = 4;   
    public List<List<GameObject>> tiles = new List<List<GameObject>>();
    public int[,] grid;
    public bool[,] visited;

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
    // Generación de la base del grid para el laberinto
    public void GenerateGrid()
    {
        tiles.Clear();

        grid = new int[rows, columns];

        



        for (int y = 0; y < rows; y++)
        {
            tiles.Add(new List<GameObject>());

            for (int x = 0; x < columns; x++)
            {
                Debug.Log(transform.position.x + (x * tileSize));
                Debug.Log(transform.position.z + (y * tileSize));
                Vector3 pos = new Vector3(
                    (x * tileSize),
                    0,
                    (y * tileSize));

                // Instanciar como hijo del GameObject que tiene el script
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
                tile.transform.parent = transform;
                tiles[y].Add(tile);
            }
        }


    }
    // Generación de laberinto 

    public void GenerateMaze()
    {
        visited = new bool[rows, columns];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                ReplaceTile(y, x, wallPrefab);
            }
        }


    }

    private void Carve(int y, int x)
    {
        visited[y, x] = true;
        List<Vector2Int> direccion = new List<Vector2Int>(directions);
        Shuffle(direccion);

        foreach (Vector2Int dir in direccion)
        {
            int ny = y + dir.y;
            int nx = x + dir.x;

            if (Inside(ny,nx) && !visited[ny, nx]){
                ReplaceTile(y, x, floorPrefab);
                ReplaceTile(ny, nx, floorPrefab);
                Carve(ny, nx);
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

    public void IncludeObstacles()
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Ejemplo simple: marcar corners
                if ((y + x) % 10 == 0 && tiles[y][x].CompareTag("Ground"))
                {
                    // Aquí puedes instanciar cofres, trampas, props, etc.
                    // Instantiate(someObject, tiles[y][x].transform.position, Quaternion.identity);
                }
            }
        }
    }
}