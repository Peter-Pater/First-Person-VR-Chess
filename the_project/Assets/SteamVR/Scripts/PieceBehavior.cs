﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceBehavior : MonoBehaviour
{
    // Start is called before the first frame update
    public string piece_identity;

    // control states
    const int POINTER_IN = 1;
    const int POINTER_OUT = 2;
    const int POINTER_CLICK = 3;

    public int control_state;

    Vector3 original_pos;
    Vector3 float_pos;


    // piece movement
    public float movement_speed = 1;

    // state machine
    StateMachine stateMachine;

    const int PLAYER_ONE_SELECT = 1;
    const int PLAYER_ONE_MOVE = 2;
    const int PLAYER_ONE_TRANSPERSPECTIVE = 3;
    const int OPPONENT_MOVES = 4;

    void Start()
    {
        control_state = POINTER_OUT;
        original_pos = this.transform.position;
        float_pos = this.transform.position + Vector3.up / 5;

        stateMachine = GameObject.Find("Player").GetComponent<StateMachine>();
    }

    // Update is called once per frame
    void Update()
    {
        laser_hover();
    }

    void laser_hover()
    {
        switch (control_state)
        {
            case POINTER_IN:
                if (this.transform.position != float_pos)
                {
                    this.transform.position = Vector3.MoveTowards(this.transform.position, float_pos, Time.deltaTime * movement_speed);
                }
                else
                {
                    //Debug.Log("Float complete!");
                }
                break;
            case POINTER_OUT:
                if (this.transform.position != original_pos)
                {
                    this.transform.position = Vector3.MoveTowards(this.transform.position, original_pos, Time.deltaTime * movement_speed);
                }
                else
                {
                    //Debug.Log("Drop complete!");
                }
                break;
            case POINTER_CLICK:
                if (stateMachine.STATE == PLAYER_ONE_SELECT)
                {
                    this.GetComponent<MeshRenderer>().enabled = false;
                    this.GetComponent<Collider>().enabled = false;
                    stateMachine.STATE = PLAYER_ONE_TRANSPERSPECTIVE;
                    control_state = POINTER_OUT;
                }
                break;
            default:
                Debug.Log("Impossible value for control state!");
                break;
        }
        
    }

}
