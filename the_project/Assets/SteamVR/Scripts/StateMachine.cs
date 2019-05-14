using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StateMachine : MonoBehaviour
{
    //States
    const int START_GAME = 0;
    const int PLAYER_ONE_SELECT = 1;
    const int PLAYER_ONE_MOVE = 2;
    const int PLAYER_ONE_TRANSPERSPECTIVE = 3;
    const int PLAYER_ONE_TRANSPERSPECTIVE_1 = 4;
    const int OPPONENT_MOVES = 5;
    const int END_GAME = 6;

    public int STATE;

    public GameObject[,] board = new GameObject[8, 8];
    public GameObject[] white_pieces = new GameObject[8];
    public GameObject[] white_pawns = new GameObject[8];
    public GameObject[] black_pieces = new GameObject[8];
    public GameObject[] black_pawns = new GameObject[8];


    // Start is called before the first frame update
    void Start()
    {
        STATE = START_GAME;
        for (int i = 0; i < 8; i++)
        {
            board[i, 7] = white_pieces[7 - i];
            board[i, 6] = white_pawns[7 - i];
            board[i, 1] = black_pawns[7 - i];
            board[i, 0] = black_pieces[7 - i];
        }


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
