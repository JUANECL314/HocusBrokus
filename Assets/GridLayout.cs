using UnityEngine;

public class GridLayout : MonoBehaviour
{
    public GameObject tilePrefab;
    public int rows = 5;
    public int columns = 5;
    public float spacing = 0f;
    public int sizeX = 4;
    public int sizeZ = 4;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateGrid();
    }


    void GenerateGrid()
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
    }

}


