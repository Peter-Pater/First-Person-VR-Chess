using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StateMachine : MonoBehaviour
{
    //States
    const int PLAYER_ONE_SELECT = 1;
    const int PLAYER_ONE_MOVE = 2;
    const int PLAYER_ONE_TRANSPERSPECTIVE = 3;
    const int OPPONENT_MOVES = 4;

    public int STATE;

    // Start is called before the first frame update
    void Start()
    {
        STATE = PLAYER_ONE_SELECT;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
