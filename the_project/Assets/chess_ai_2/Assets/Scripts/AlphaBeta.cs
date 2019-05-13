﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AlphaBeta
{
    int maxDepth = 3;

    List<Move_new> _moves = new List<Move_new>();
    List<Tile> _tilesWithPieces = new List<Tile>();
    List<Tile> _blackPieces = new List<Tile>();
    List<Tile> _whitePieces = new List<Tile>();
    Stack<Move_new> moveStack = new Stack<Move_new>();
    Weights _weight = new Weights();
    Tile[,] _localBoard_new = new Tile[8,8];
    int _whiteScore = 0;
    int _blackScore = 0;
    Move_new bestMove;

    Board_new _board;

    public Move_new GetMove()
    {
        _board = Board_new.Instance;
        bestMove = _CreateMove(_board.GetTileFromBoard(new Vector2(0, 0)), _board.GetTileFromBoard(new Vector2(0, 0)));
        AB(maxDepth, -100000000, 1000000000, true);
        return bestMove;
    }

    int AB(int depth, int alpha, int beta, bool max)
    {
        _GetBoard_newState();

        if (depth == 0)
        {
            return _Evaluate();
        }
        if (max)
        {
            int score = -10000000;
            List<Move_new> allMoves = _GetMoves(Piece.playerColor.BLACK);
            foreach (Move_new move in allMoves)
            {
                moveStack.Push(move);

                _DoFakeMove(move.firstPosition, move.secondPosition);

                score = AB(depth - 1, alpha, beta, false);

                _UndoFakeMove();

                if (score > alpha)
                {
                    move.score = score;
                    if (move.score > bestMove.score && depth == maxDepth)
                    {
                        bestMove = move;
                    }
                    alpha = score;
                }
                if (score >= beta)
                {
                    break;
                }
            }
            return alpha;
        }
        else
        {
            int score = 10000000;
            List<Move_new> allMoves = _GetMoves(Piece.playerColor.WHITE);
            foreach (Move_new move in allMoves)
            {
                moveStack.Push(move);

                _DoFakeMove(move.firstPosition, move.secondPosition);

                score = AB(depth - 1, alpha, beta, true);

                _UndoFakeMove();

                if (score < beta)
                {
                    move.score = score;
                    beta = score;
                }
                if (score <= alpha)
                {
                    break;
                }
            }
            return beta;
        }
    }

    void _UndoFakeMove()
    {
        Move_new tempMove = moveStack.Pop();
        Tile movedTo = tempMove.secondPosition;
        Tile movedFrom = tempMove.firstPosition;
        Piece pieceKilled = tempMove.pieceKilled;
        Piece pieceMoved = tempMove.pieceMoved;

        movedFrom.CurrentPiece = movedTo.CurrentPiece;

        if (pieceKilled != null)
        {
            movedTo.CurrentPiece = pieceKilled;
        }
        else
        {
            movedTo.CurrentPiece = null;
        }
    }

    void _DoFakeMove(Tile currentTile, Tile targetTile)
    {
        targetTile.SwapFakePieces(currentTile.CurrentPiece);
        currentTile.CurrentPiece = null;
    }

    List<Move_new> _GetMoves(Piece.playerColor color)
    {
        List<Move_new> turnMove = new List<Move_new>();
        List<Tile> pieces = new List<Tile>();

        if (color == Piece.playerColor.BLACK)
            pieces = _blackPieces;
        else pieces = _whitePieces;

        foreach (Tile tile in pieces)
        {
            MoveFactory factory = new MoveFactory(_board);
            List<Move_new> pieceMoves = factory.GetMoves(tile.CurrentPiece, tile.Position);

            foreach(Move_new move in pieceMoves)
            {
                Move_new newMove = _CreateMove(move.firstPosition, move.secondPosition);
                turnMove.Add(newMove);
            }
        }
        return turnMove;
    }

    int _Evaluate()
    {
        float pieceDifference = 0;
        float whiteWeight = 0;
        float blackWeight = 0;

        foreach(Tile tile in _whitePieces)
        {
            whiteWeight += _weight.GetBoardWeight(tile.CurrentPiece.Type, tile.CurrentPiece.position, Piece.playerColor.WHITE);
        }
        foreach (Tile tile in _blackPieces)
        {
            blackWeight += _weight.GetBoardWeight(tile.CurrentPiece.Type, tile.CurrentPiece.position, Piece.playerColor.BLACK);
        }
        pieceDifference = (_blackScore + (blackWeight / 100)) - (_whiteScore + (whiteWeight / 100));
        return Mathf.RoundToInt(pieceDifference * 100);
    }

    void _GetBoard_newState()
    {
        _blackPieces.Clear();
        _whitePieces.Clear();
        _blackScore = 0;
        _whiteScore = 0;
        _tilesWithPieces.Clear();

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                _localBoard_new[x, y] = _board.GetTileFromBoard(new Vector2(x, y));
                if (_localBoard_new[x, y].CurrentPiece != null && _localBoard_new[x, y].CurrentPiece.Type != Piece.pieceType.UNKNOWN)
                {
                    _tilesWithPieces.Add(_localBoard_new[x, y]);
                }
            }
        }
        foreach (Tile tile in _tilesWithPieces)
        {
            if (tile.CurrentPiece.Player == Piece.playerColor.BLACK)
            {
                _blackScore += _weight.GetPieceWeight(tile.CurrentPiece.Type);
                _blackPieces.Add(tile);
            }
            else
            {
                _whiteScore += _weight.GetPieceWeight(tile.CurrentPiece.Type);
                _whitePieces.Add(tile);
            }
        }
    }

    Move_new _CreateMove(Tile tile, Tile move)
    {
        Move_new tempMove = new Move_new();
        tempMove.firstPosition = tile;
        tempMove.pieceMoved = tile.CurrentPiece;
        tempMove.secondPosition = move;
        
        if (move.CurrentPiece != null)
        {
            tempMove.pieceKilled = move.CurrentPiece;
        }

        return tempMove;
    }
}
