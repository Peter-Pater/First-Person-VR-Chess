using UnityEngine;
using System.Collections;

public class Tile_new
{
    private Vector2 _position = Vector2.zero;
    public Vector2 Position
    {
        get { return _position; }
    }

    private Piece_new _currentPiece = null;
    public Piece_new CurrentPiece
    {
        get { return _currentPiece; }
        set { _currentPiece = value; }
    }

    public Tile_new(int x, int y)
    {
        _position.x = x;
        _position.y = y;

        if (y == 0 || y == 1 || y == 6 || y == 7)
        {
            _currentPiece = GameObject.Find(x.ToString() + " " + y.ToString()).GetComponent<Piece_new>();
        }
    }

    public void SwapFakePieces(Piece_new newPiece)
    {
        _currentPiece = newPiece;
    }
}
