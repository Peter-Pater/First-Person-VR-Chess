using UnityEngine;
using System.Collections;

public class Board_new
{
    private static Board_new _instance = null;
    public static Board_new Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Board_new();
            }
            return _instance;
        }
    }

    private Tile_new[,] _board = new Tile_new[8, 8];

    public void SetupBoard()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                _board[x, y] = new Tile_new(x, y);
            }
        }
    }

    public Tile_new GetTileFromBoard(Vector2 tile)
    {
        return _board[(int)tile.x, (int)tile.y];
    }
}
