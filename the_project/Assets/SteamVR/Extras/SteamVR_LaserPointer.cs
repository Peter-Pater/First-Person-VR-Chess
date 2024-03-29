﻿//======= Copyright (c) Valve Corporation, All rights reserved. ===============
using UnityEngine;
using System.Collections;

namespace Valve.VR.Extras
{
    public class SteamVR_LaserPointer : MonoBehaviour
    {
        public SteamVR_Behaviour_Pose pose;

        //public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.__actions_default_in_InteractUI;
        public SteamVR_Action_Boolean interactWithUI = SteamVR_Input.GetBooleanAction("InteractUI");

        public bool active = true;
        public Color color;
        public float thickness = 0.002f;
        public Color clickColor = Color.green;
        public GameObject holder;
        public GameObject pointer;
        bool isActive = false;
        public bool addRigidBody = false;
        public Transform reference;
        public event PointerEventHandler PointerIn;
        public event PointerEventHandler PointerOut;
        public event PointerEventHandler PointerClick;

        Transform previousContact = null;

        // player movement speed
        public float transperspective_speed = 6;
        public float move_speed = 1;

        // control states
        const int POINTER_IN = 1;
        const int POINTER_OUT = 2;
        const int POINTER_CLICK = 3;

        int control_state = 0;

        // global states
        const int START_GAME = 0;
        const int PLAYER_ONE_SELECT = 1;
        const int PLAYER_ONE_MOVE = 2;
        const int PLAYER_ONE_TRANSPERSPECTIVE = 3;
        const int PLAYER_ONE_TRANSPERSPECTIVE_1 = 4;
        const int OPPONENT_MOVES = 5;

        GameObject current_piece;
        PieceBehavior current_piece_script;

        GameObject player;
        GameObject target_piece;
        GameObject target_tile;
        StateMachine stateMachine;

        int[] enemy_move = { -1, -1, -1, -1 };
        GameObject enemypiece;
        GameObject enemy_target_tile;
        Vector3 enemy_target_pos;

        BoardManager boardManager;

        // new ai
        Piece_new piece_selected;

        private void Start()
        {
            boardManager = GameObject.Find("ChessManager").GetComponent<BoardManager>();

            if (pose == null)
                pose = this.GetComponent<SteamVR_Behaviour_Pose>();
            if (pose == null)
                Debug.LogError("No SteamVR_Behaviour_Pose component found on this object");
            
            if (interactWithUI == null)
                Debug.LogError("No ui interaction action has been set on this component.");

            current_piece = GameObject.Find("king_white");
            current_piece_script = current_piece.GetComponent<PieceBehavior>();

            target_piece = current_piece;
            player = GameObject.Find("Player");
            current_piece.GetComponent<MeshRenderer>().enabled = false;
            stateMachine = player.GetComponent<StateMachine>();

            holder = new GameObject();
            holder.transform.parent = this.transform;
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;

            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.transform.parent = holder.transform;
            pointer.transform.localScale = new Vector3(thickness, thickness, 100f);
            pointer.transform.localPosition = new Vector3(0f, 0f, 50f);
            pointer.transform.localRotation = Quaternion.identity;
            BoxCollider collider = pointer.GetComponent<BoxCollider>();
            if (addRigidBody)
            {
                if (collider)
                {
                    collider.isTrigger = true;
                }
                Rigidbody rigidBody = pointer.AddComponent<Rigidbody>();
                rigidBody.isKinematic = true;
            }
            else
            {
                if (collider)
                {
                    Object.Destroy(collider);
                }
            }
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));
            newMaterial.SetColor("_Color", color);
            pointer.GetComponent<MeshRenderer>().material = newMaterial;
        }

        public virtual void OnPointerIn(PointerEventArgs e)
        {
            if (PointerIn != null)
            {
                PointerIn(this, e);
            }
        }

        public virtual void OnPointerClick(PointerEventArgs e)
        {
            if (PointerClick != null)
                PointerClick(this, e);
        }

        public virtual void OnPointerOut(PointerEventArgs e)
        {
            if (PointerOut != null)
                PointerOut(this, e);
        }

        
        private void Update()
        {
            if (!isActive)
            {
                isActive = true;
                this.transform.GetChild(0).gameObject.SetActive(true);
            }

            float dist = 100f;

            Ray raycast = new Ray(transform.position, transform.forward);
            RaycastHit hit;
            bool bHit = Physics.Raycast(raycast, out hit);


            // Pointer out
            if (previousContact && previousContact != hit.transform)
            {
                PointerEventArgs args = new PointerEventArgs();
                args.fromInputSource = pose.inputSource;
                args.distance = 0f;
                args.flags = 0;
                args.target = previousContact;
                OnPointerOut(args);
                previousContact = null;
                LaserEventHandler(args, POINTER_OUT);
                control_state = POINTER_OUT;
            }

            // Pointer in
            if (bHit && previousContact != hit.transform)
            {
                PointerEventArgs argsIn = new PointerEventArgs();
                argsIn.fromInputSource = pose.inputSource;
                argsIn.distance = hit.distance;
                argsIn.flags = 0;
                argsIn.target = hit.transform;
                OnPointerIn(argsIn);
                //Debug.Log("Current: " + hit.transform.name);
                //Debug.Log("Previous: " + previousContact.name);
                previousContact = hit.transform;
                LaserEventHandler(argsIn, POINTER_IN);
                control_state = POINTER_IN;
            }


            if (!bHit)
            {
                previousContact = null;
            }


            if (bHit && hit.distance < 100f)
            {
                dist = hit.distance;
            }

            // Pointer click
            if (bHit && interactWithUI.GetStateUp(pose.inputSource))
            {
                PointerEventArgs argsClick = new PointerEventArgs();
                argsClick.fromInputSource = pose.inputSource;
                argsClick.distance = hit.distance;
                argsClick.flags = 0;
                argsClick.target = hit.transform;
                OnPointerClick(argsClick);
                LaserEventHandler(argsClick, POINTER_CLICK);
                control_state = POINTER_CLICK;
            }

            if (interactWithUI != null && interactWithUI.GetState(pose.inputSource))
            {
                pointer.transform.localScale = new Vector3(thickness * 5f, thickness * 5f, dist);
                pointer.GetComponent<MeshRenderer>().material.color = clickColor;
            }
            else
            {
                pointer.transform.localScale = new Vector3(thickness, thickness, dist);
                pointer.GetComponent<MeshRenderer>().material.color = color;
            }
            pointer.transform.localPosition = new Vector3(0f, 0f, dist / 2f);

            PerspectiveHandler();

            control_state = 0;
        }

        public void LaserEventHandler(PointerEventArgs element, int control_state)
        {
            GameObject target = element.target.gameObject;
            if (stateMachine.STATE == PLAYER_ONE_SELECT)
            {
                // is the element a piece?
                if (target.GetComponent<PieceBehavior>() && target != current_piece)
                {
                    target.GetComponent<PieceBehavior>().control_state = control_state;
                    if (control_state == POINTER_CLICK)
                    {
                        target_piece = element.target.gameObject;
                    }
                }
                else
                {
                    // is it a tile?
                    if (control_state == POINTER_CLICK)
                    {
                        target_tile = element.target.gameObject;
                        foreach (Position pos in boardManager.possibleMovePositions)
                        {
                            string tileName = GetTileName(pos.x, pos.y);
                            //Debug.Log(tileName);
                            GameObject current_tile = GameObject.Find(tileName);
                            if (target_tile.name == current_tile.name)
                            {
                                // update board information here
                                update_board(current_piece.GetComponent<PieceBehavior>().x,
                                             current_piece.GetComponent<PieceBehavior>().y,
                                             pos.x,
                                             pos.y);
                                current_piece.GetComponent<PieceBehavior>().x = pos.x;
                                current_piece.GetComponent<PieceBehavior>().y = pos.y;
                                stateMachine.STATE = PLAYER_ONE_TRANSPERSPECTIVE_1;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void PerspectiveHandler()
        {
            // TO DO: RECONSIDER THIS STATE
            if (stateMachine.STATE == START_GAME)
            {
                current_piece_script = current_piece.GetComponent<PieceBehavior>();
                boardManager.makeMove(current_piece_script.x, current_piece_script.y);
                DrawTile(1);
                stateMachine.STATE = PLAYER_ONE_SELECT;
            }
            if (stateMachine.STATE == PLAYER_ONE_SELECT)
            {
                player.transform.position = current_piece.transform.position + Vector3.up;
            }
            if (stateMachine.STATE == PLAYER_ONE_TRANSPERSPECTIVE)
            {
                if (player.transform.position != target_piece.transform.position + Vector3.up)
                {
                    //player.transform.position = Vector3.MoveTowards(player.transform.position, target_piece.transform.position + Vector3.up, transperspective_speed * Time.deltaTime);
                    player.transform.position = target_piece.transform.position + Vector3.up;
                }
                else
                {
                    //Debug.Log("Transperspective complete!");
                    stateMachine.STATE = PLAYER_ONE_SELECT;
                    DrawTile(0);
                    current_piece.GetComponent<MeshRenderer>().enabled = true;
                    current_piece.GetComponent<Collider>().enabled = true;
                    current_piece = target_piece;

                    // highlight possible moves
                    current_piece_script = current_piece.GetComponent<PieceBehavior>();
                    boardManager.makeMove(current_piece_script.x, current_piece_script.y);
                    DrawTile(1);
                }
            }
            if (stateMachine.STATE == PLAYER_ONE_MOVE)
            {
                    
            }
            if (stateMachine.STATE == PLAYER_ONE_TRANSPERSPECTIVE_1)
            {
                Vector3 target_pos_1 = new Vector3(target_tile.transform.position.x,
                                                player.transform.position.y,
                                                    target_tile.transform.position.z);
                Vector3 target_pos_2 = new Vector3(target_tile.transform.position.x,
                                                current_piece.transform.position.y,
                                                    target_tile.transform.position.z);
                current_piece.GetComponent<PieceBehavior>().original_pos = target_pos_2;
                current_piece.GetComponent<PieceBehavior>().float_pos = current_piece.GetComponent<PieceBehavior>().original_pos + Vector3.up / 5;
                if (player.transform.position != target_pos_1 || current_piece.transform.position != target_pos_2)
                {
                    player.transform.position = Vector3.MoveTowards(player.transform.position,
                                                                target_pos_1,
                                                                transperspective_speed * Time.deltaTime);
                    current_piece.transform.position = Vector3.MoveTowards(current_piece.transform.position,
                                                                target_pos_2,
                                                                transperspective_speed * Time.deltaTime);
                }
                else
                {
                    //stateMachine.STATE = PLAYER_ONE_SELECT;
                    stateMachine.STATE = OPPONENT_MOVES;
                    boardManager.makeMove(current_piece_script.x, current_piece_script.y);
                    enemy_move = boardManager.decideAndMoveEnemyPiece();
                    DrawTile(0);
                }
            }
            if (stateMachine.STATE == OPPONENT_MOVES)
            {
                enemypiece = stateMachine.board[enemy_move[0], enemy_move[1]];
                enemy_target_tile = GameObject.Find(GetTileName(enemy_move[2], enemy_move[3]));

                enemy_target_pos = new Vector3(enemy_target_tile.transform.position.x, 
                                                enemypiece.transform.position.y, 
                                                enemy_target_tile.transform.position.z);
                if (enemypiece.transform.position != enemy_target_pos)
                {
                    enemypiece.transform.position = Vector3.MoveTowards(enemypiece.transform.position, enemy_target_pos, transperspective_speed * Time.deltaTime);
                }
                else
                {
                    //Debug.Log("Enemy move completes!");
                    stateMachine.STATE = PLAYER_ONE_SELECT;
                    update_board(enemy_move[0], enemy_move[1], enemy_move[2], enemy_move[3]);
                    boardManager.resetTiles();
                    current_piece_script = current_piece.GetComponent<PieceBehavior>();
                    boardManager.makeMove(current_piece_script.x, current_piece_script.y);
                    DrawTile(1);
                }
            }
        }

        public string GetTileName(int x, int y)
        {
            string[] name_x = { "H", "G", "F", "E", "D", "C", "B", "A" };
            string[] name_y = { "1", "2", "3", "4", "5", "6", "7", "8" };
            return name_x[x] + name_y[y];
        }

        public void DrawTile(int matnumber)
        {
            foreach (Position pos in boardManager.possibleMovePositions)
            {
                string tileName = GetTileName(pos.x, pos.y);
                //Debug.Log(tileName);
                GameObject current_tile = GameObject.Find(tileName);
                MaterialControl current_matcontrol = current_tile.GetComponent<MaterialControl>();
                Material[] mat = { current_matcontrol.material_list[matnumber] };
                current_tile.GetComponent<MeshRenderer>().materials = mat;
            }

            piece_selected = GameObject.Find("0 6").GetComponent<Piece_new>();
            piece_selected.Selected();

            foreach(Move_new move in piece_selected.moves)
            {
                Debug.Log(move.secondPosition.Position.x);
                Debug.Log(move.secondPosition.Position.y);
            }
        }

        public void update_board(int x1, int y1, int x2, int y2)
        {
            stateMachine.board[x2, y2] = stateMachine.board[x1, y1];
            stateMachine.board[x1, y1] = null;
        }
    }


    public struct PointerEventArgs
    {
        public SteamVR_Input_Sources fromInputSource;
        public uint flags;
        public float distance;
        public Transform target;
    }

    public delegate void PointerEventHandler(object sender, PointerEventArgs e);
}