using System;
using System.Collections;
using System.Collections.Generic;
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
    public int rows = 21;
    public int columns = 15;
    public int tileSize = 4;

    // tiles[y][x]
    public List<List<GameObject>> tiles = new List<List<GameObject>>();
    public bool[,] visited;

    // Start / Goal
    private int longestDistance = 0;
    public Vector2Int goalNode = new Vector2Int(14, 20); // puede recalcularse
    public Vector2Int startNode = new Vector2Int(1, 0);

    // 0 = wall, 1 = floor, 2 = goal
    public int[,] mazeData;

    [Header("Obstacles")]
    public GameObject[] obstacles; // prefabs (local instantiation)
    [Header("Obstacles Settings")]
    public float obstacleProbability = 0.15f;
    public int minObstacles = 4;

    // Obstacle data lista (solo Master genera y luego envía)
    private List<ObstacleData> obstacleList = new List<ObstacleData>();

    [Serializable]
    struct ObstacleData
    {
        public byte typeIndex;
        public byte x;
        public byte y;
        public ObstacleData(byte typeIndex, byte x, byte y)
        {
            this.typeIndex = typeIndex;
            this.x = x;
            this.y = y;
        }
    }

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
        // inicializar mazeData en Awake con dimensiones actuales
        mazeData = new int[rows, columns];
    }

    void Start()
    {
        // nada por defecto; GenerateGrid() lo manejará
    }

    // -------------------------
    // CREACIÓN BASE DE LA GRILLA (solo crea paredes iniciales)
    // -------------------------
    void CreateBaseGridLocal()
    {
        // destruir cualquier cosa previa
        DestroyMaze();

        mazeData = new int[rows, columns];
        tiles = new List<List<GameObject>>();

        for (int y = 0; y < rows; y++)
        {
            tiles.Add(new List<GameObject>());

            for (int x = 0; x < columns; x++)
            {
                Vector3 pos = new Vector3(
                    transform.position.x + (x * tileSize),
                    wallPrefab.transform.position.y,
                    transform.position.z + (y * tileSize));

                GameObject tile = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                tiles[y].Add(tile);
                mazeData[y, x] = 0; // marcar como muro por defecto
            }
        }
    }

    public void ReplaceTile(int y, int x, GameObject prefab)
    {
        if (y < 0 || y >= rows || x < 0 || x >= columns) return;

        GameObject old = tiles[y][x];
        int type = 0;
        if (prefab == floorPrefab) type = 1;
        else if (prefab == goalPrefab) type = 2;

        mazeData[y, x] = type;

        float altura = (prefab.tag == "Floor" || prefab.tag == "Goal") ? 1f : prefab.transform.position.y;
        Vector3 pos = new Vector3(old.transform.position.x, altura, old.transform.position.z);

        GameObject newTile = Instantiate(prefab, pos, Quaternion.identity, transform);
        tiles[y][x] = newTile;

        if (Application.isPlaying) Destroy(old);
        else DestroyImmediate(old);
    }

    public void DestroyMaze()
    {
        // eliminar children
        List<GameObject> toDelete = new List<GameObject>();
        foreach (Transform child in transform)
            toDelete.Add(child.gameObject);

        foreach (GameObject go in toDelete)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        tiles.Clear();
    }

    // -------------------------
    // Método público para que el Master genere todo
    // -------------------------
    public void GenerateMazeMaster()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Solo el MasterClient debe llamar GenerateMazeMaster()");
            return;
        }

        // 1) Crear grilla base (paredes)
        CreateBaseGridLocal();

        // 2) Inicializar visited y demás
        visited = new bool[rows, columns];
        longestDistance = 0;
        obstacleList.Clear();

        // asegurar start dentro
        startNode.x = Mathf.Clamp(startNode.x, 0, columns - 1);
        startNode.y = Mathf.Clamp(startNode.y, 0, rows - 1);

        // marcar start como floor
        ReplaceTile(startNode.y, startNode.x, floorPrefab);

        // 3) Carve con DFS recursivo
        CarveRecursive(startNode.y, startNode.x, 0);

        // 4) Marcar goal en el nodo más lejano calculado por CarveRecursive
        ReplaceTile(goalNode.y, goalNode.x, goalPrefab);
        mazeData[goalNode.y, goalNode.x] = 2;

        // 5) Spawn obstáculos SOLO en Master (no Network.Instantiate por cada uno)
        SpawnObstacles_MasterOnly();

        // 6) Enviar todo en un único RPC ligero (maze + obstacles)
        SendMazeToClients();
    }

    // -------------------------
    // SERIALIZACIÓN y ENVÍO
    // -------------------------
    private void SendMazeToClients()
    {
        int[] flat = SerializeMaze(mazeData);

        int obsCount = obstacleList.Count;
        byte[] obsTypes = new byte[obsCount];
        byte[] obsXs = new byte[obsCount];
        byte[] obsYs = new byte[obsCount];

        for (int i = 0; i < obsCount; i++)
        {
            obsTypes[i] = obstacleList[i].typeIndex;
            obsXs[i] = obstacleList[i].x;
            obsYs[i] = obstacleList[i].y;
        }

        // RPC único y buffered para clientes futuros
        photonView.RPC(nameof(RPC_ReceiveMaze), RpcTarget.OthersBuffered, flat, obsTypes, obsXs, obsYs, rows, columns);
    }

    int[] SerializeMaze(int[,] data)
    {
        int r = data.GetLength(0);
        int c = data.GetLength(1);
        int[] flat = new int[r * c];
        for (int y = 0; y < r; y++)
            for (int x = 0; x < c; x++)
                flat[y * c + x] = data[y, x];

        return flat;
    }

    int[,] DeserializeMaze(int[] flat, int r, int c)
    {
        int[,] d = new int[r, c];
        for (int y = 0; y < r; y++)
            for (int x = 0; x < c; x++)
                d[y, x] = flat[y * c + x];
        return d;
    }

    [PunRPC]
    void RPC_ReceiveMaze(int[] flatMaze, byte[] obsTypes, byte[] obsXs, byte[] obsYs, int r, int c)
    {
        // Este RPC lo reciben TODOS los clientes (excepto el Master que ya lo generó localmente).
        StartCoroutine(ApplyMazeWhenReady(flatMaze, obsTypes, obsXs, obsYs, r, c));
    }

    IEnumerator ApplyMazeWhenReady(int[] flatMaze, byte[] obsTypes, byte[] obsXs, byte[] obsYs, int r, int c)
    {
        // Esperar a que la instancia exista (en caso de orden de carga)
        while (GridLayoutBase.instance == null)
            yield return null;

        // Si dimensiones diferentes, reasignar
        rows = r;
        columns = c;

        // 1) Crear base de paredes localmente (misma lógica de posiciones)
        CreateBaseGridLocal();

        // 2) Reconstruir mazeData desde arreglo serializado
        mazeData = DeserializeMaze(flatMaze, rows, columns);

        // 3) Aplicar mazeData: reemplazar tiles correspondientes
        ApplyMazeData(mazeData);

        // 4) Instanciar obstáculos localmente según arrays (sin PhotonNetwork.Instantiate)
        InstantiateObstaclesLocal(obsTypes, obsXs, obsYs);
    }

    // -------------------------
    // Obstáculos: Master los crea en datos y los instancia localmente
    // -------------------------
    private void SpawnObstacles_MasterOnly()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (obstacles == null || obstacles.Length == 0) return;

        List<Vector2Int> floorTiles = new List<Vector2Int>();
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                if (mazeData[y, x] == 1) // floor
                {
                    if (x == startNode.x && y == startNode.y) continue;
                    if (x == goalNode.x && y == goalNode.y) continue;
                    floorTiles.Add(new Vector2Int(x, y));
                }

        int placed = 0;
        System.Random rnd = new System.Random(); // opcional: determinista si quieres seed
        for (int i = floorTiles.Count - 1; i >= 0; i--)
        {
            Vector2Int tile = floorTiles[i];
            if (UnityEngine.Random.value <= obstacleProbability)
            {
                // elegir tipo aleatorio
                int typeIndex = UnityEngine.Random.Range(0, obstacles.Length);
                obstacleList.Add(new ObstacleData((byte)typeIndex, (byte)tile.x, (byte)tile.y));
                // Instanciar localmente (Master)
                PlaceObstacleLocal(tile.y, tile.x, typeIndex);
                placed++;
            }
        }

        // asegurar mínimo
        while (placed < minObstacles && floorTiles.Count > 0)
        {
            int i = UnityEngine.Random.Range(0, floorTiles.Count);
            Vector2Int tile = floorTiles[i];
            int typeIndex = UnityEngine.Random.Range(0, obstacles.Length);
            obstacleList.Add(new ObstacleData((byte)typeIndex, (byte)tile.x, (byte)tile.y));
            PlaceObstacleLocal(tile.y, tile.x, typeIndex);
            floorTiles.RemoveAt(i);
            placed++;
        }
    }

    private void PlaceObstacleLocal(int y, int x, int typeIndex)
    {
        // colocar obstáculo en la posición del tile y,x (no modificamos mazeData: sigue siendo floor)
        GameObject tile = tiles[y][x];
        if (tile == null) return;

        GameObject prefab = obstacles[typeIndex];
        Vector3 pos = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.2f, tile.transform.position.z);
        GameObject obs = Instantiate(prefab, pos, Quaternion.identity, transform);

        // ajustes por tipo (mantengo tu switch)
        string name = prefab.name;
        Vector3 escala;
        switch (name)
        {
            case "VortexObstacle":
                escala = new Vector3(0.7897533f, 0.5265021f, 0.3948766f);
                break;
            case "FieryObstacle":
                escala = new Vector3(0.2151325f, 0.2151325f, 0.2151325f);
                break;
            case "RockObstacle":
                escala = new Vector3(1f, 5.80744f, 1f);
                float resto = 9.87f - tile.transform.position.y;
                pos = new Vector3(tile.transform.position.x, tile.transform.position.y + resto, tile.transform.position.z - 2);
                obs.transform.position = pos;
                break;
            case "TreeObstacle":
                escala = new Vector3(1f, 2.9f, 1f);
                break;
            default:
                escala = new Vector3(1f, 1f, 1f);
                break;
        }
        obs.transform.localScale = escala;
        obs.name = $"Obs_{typeIndex}_{x}_{y}";
    }

    // para clientes
    private void InstantiateObstaclesLocal(byte[] obsTypes, byte[] obsXs, byte[] obsYs)
    {
        if (obsTypes == null || obsTypes.Length == 0) return;
        int n = obsTypes.Length;
        for (int i = 0; i < n; i++)
        {
            int type = obsTypes[i];
            int x = obsXs[i];
            int y = obsYs[i];
            if (type < 0 || type >= obstacles.Length) continue;
            PlaceObstacleLocal(y, x, type);
        }
    }

    // -------------------------
    // CARVE DFS
    // -------------------------
    private void CarveRecursive(int y, int x, int distance)
    {
        visited[y, x] = true;

        // actualizar mayor distancia y goalNode
        if (distance > longestDistance)
        {
            longestDistance = distance;
            goalNode = new Vector2Int(x, y);
        }

        // mezclar direcciones
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
                // abrir muro entre
                ReplaceTile(betweenY, betweenX, floorPrefab);
                // abrir celda destino
                ReplaceTile(ny, nx, floorPrefab);
                // recursivo
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
        return y > 0 && y < rows - 1 && x > 0 && x < columns - 1;
    }

    private void MarkGoalNode()
    {
        // ya hacemos ReplaceTile(goalNode.y, goalNode.x, goalPrefab) en GenerateMaster
    }

    // -------------------------
    // APPLY desde datos (clientes)
    // -------------------------
    public void ApplyMazeData(int[,] data)
    {
        mazeData = data;

        // reemplazar según matrix
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
