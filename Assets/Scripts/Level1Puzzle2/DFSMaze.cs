using UnityEngine;
using System.Collections.Generic;

public class DFSMaze : IMazeAlgorithm
{
    private bool[,] visited;

    private static readonly List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(0,1),
        new Vector2Int(0,-1),
        new Vector2Int(1,0),
        new Vector2Int(-1,0),
    };


    public void Generate(GridLayoutBase grid)
    {
        int rows = grid.rows;
        int cols = grid.columns;

        visited = new bool[rows, cols];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                grid.ReplaceTile(y, x, grid.wallPrefab);
            }
        }
    }

    
}
