using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    AlphaBeta ab = new AlphaBeta();
    private bool _kingDead = false;
    Board_new _board;

	void Start ()
    {
        _board = Board_new.Instance;
        _board.SetupBoard();
	}

	void Update ()
    {
        if (_kingDead)
        {
            //Debug.Log("WINNER!");
            //UnityEditor.EditorApplication.isPlaying = false;
            //Application.Quit();
        }

        if (!playerTurn)
        {
            //Move_new move = ab.GetMove();
            //Debug.Log(move.secondPosition.Position.x);
            //Debug.Log(move.secondPosition.Position.y);
            //_DoAIMove(move);
        }
	}

    public bool playerTurn = true;

    public void DoAIMove(Move_new move)
    {
        Tile_new firstPosition = move.firstPosition;
        Tile_new secondPosition = move.secondPosition;

        if (secondPosition.CurrentPiece && secondPosition.CurrentPiece.Type == Piece_new.pieceType.KING)
        {
            SwapPieces(move);
            _kingDead = true;
        }
        else
        {
            SwapPieces(move);
        }
    }

    public void SwapPieces(Move_new move)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Highlight");
        foreach (GameObject o in objects)
        {
            Destroy(o);
        }

        Tile_new firstTile = move.firstPosition;
        Tile_new secondTile = move.secondPosition;

        firstTile.CurrentPiece.MovePiece(new Vector3(-move.secondPosition.Position.x, 0, move.secondPosition.Position.y));

        if (secondTile.CurrentPiece != null)
        {
            if (secondTile.CurrentPiece.Type == Piece_new.pieceType.KING)
                _kingDead = true;
            Destroy(secondTile.CurrentPiece.gameObject);
        }
            

        secondTile.CurrentPiece = move.pieceMoved;
        firstTile.CurrentPiece = null;
        secondTile.CurrentPiece.position = secondTile.Position;
        secondTile.CurrentPiece.HasMoved = true;

        playerTurn = !playerTurn;
    }
}
