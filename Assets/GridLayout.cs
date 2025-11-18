using UnityEngine;

public class GridLayout : MonoBehaviour
{
    public static GridLayout instance;
    public GameObject tilePrefab;
    public int rows;
    public int columns;
    public float spacing = 0f;
    public int sizeX;
    public int sizeZ;
    public int[,] grid;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        grid = GenerateGrid();
    }


    public int[,] GenerateGrid()
    {
        
        
        // Tamaño del prefab
        Vector3 tileSize = tilePrefab.GetComponent<Renderer>().bounds.size;

        // Offset para centrar el grid
        Vector3 offset = new Vector3(
            (columns - 1) * (tileSize.x + spacing) / 2,
            0,
            (rows - 1) * (tileSize.z + spacing) / 2
        );

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Posición relativa al padre
                Vector3 localPosition = new Vector3(
                    x * (tileSize.x + spacing) - offset.x,
                    0,
                    y * (tileSize.z + spacing) - offset.z
                );

                // Instanciar como hijo del GameObject que tiene el script
                GameObject tile = Instantiate(tilePrefab, transform);
                tile.transform.localPosition = localPosition;
            }
        }
        int[,] matriz = new int[rows, columns];
        return matriz;
    }

}


