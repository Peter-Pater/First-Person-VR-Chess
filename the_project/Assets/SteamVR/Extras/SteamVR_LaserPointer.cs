//======= Copyright (c) Valve Corporation, All rights reserved. ===============
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
        
        public Font gameFonts;

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
        const int END_GAME = 6;

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

        //BoardManager boardManager;
        GameManager gameManager;

        AlphaBeta ab;

        // new ai
        Piece_new piece_selected;

        Move_new ai_move;
        Move_new current_move;

        bool killing = false;
        float killTime;

        Vector3 killingMove;
        Vector3 knight_height;
        bool horse_rising = true;

        Vector3 target_pos_1;
        Vector3 target_pos_2;

        // start game variables
        int lightsCounter = 0;
        float lightTime;

        public AudioClip kill_sound;
        public AudioClip horse_sound;

        public GameObject text;
        bool start = false;
        float textTime;
        int textCounter = 0;

        //string instruction = "Instruction: in this game you will play chess from a first person perspective view. You will start as the king, " +
        //    "and then you can switch between pieces as you wish. The possible moves will be highlighted as green and red," +
        //    " make your move or switch between pieces with the laser beam";

        string[] texts = { "PEOPLE KMOW THAT", "\"YOU KNOW WHO\" HAS 7 DEATHLY HALLOWS AND 8 SOULS", "WHAT PEOPLE DON'T KNOW IS",
            "THERE HAS ALWAYS BEEN ONE MORE AT LARGE", "AND HERE I AM", "TRAPPED IN THIS CHESS GAME FOR 50 YEARS", "TODAY I COME BACK TO POWER", "AND YOU ARE NO ONE TO STOP ME!", ""};

        string[] endText = new string[2];

        //int endGame = 0;

        private void Start()
        {
            //boardManager = GameObject.Find("ChessManager").GetComponent<BoardManager>();
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            knight_height = GameObject.Find("knight_black").transform.position + 2.5f * Vector3.up;

            text = GameObject.Find("Text");

            lightTime = Time.time;
            textTime = Time.time;

            ab = new AlphaBeta();

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
                        //foreach (Position pos in boardManager.possibleMovePositions)
                        //{
                        //    string tileName = GetTileName(pos.x, pos.y);
                        //    //Debug.Log(tileName);
                        //    GameObject current_tile = GameObject.Find(tileName);
                        //    if (target_tile.name == current_tile.name)
                        //    {
                        //        // update board information here
                        //        update_board(current_piece.GetComponent<PieceBehavior>().x,
                        //                     current_piece.GetComponent<PieceBehavior>().y,
                        //                     pos.x,
                        //                     pos.y);
                        //        current_piece.GetComponent<PieceBehavior>().x = pos.x;
                        //        current_piece.GetComponent<PieceBehavior>().y = pos.y;
                        //        stateMachine.STATE = PLAYER_ONE_TRANSPERSPECTIVE_1;
                        //        break;
                        //    }
                        //}

                        // AI: Selected a Tile!
                        target_tile = element.target.gameObject;
                        foreach (Move_new move in piece_selected.moves)
                        {
                            string tileName = GetTileName((int) move.secondPosition.Position.x, (int) move.secondPosition.Position.y);
                            //Debug.Log(tileName);
                            GameObject current_tile = GameObject.Find(tileName);
                            if (target_tile.name == current_tile.name)
                            {
                                // update board information here
                                current_piece.GetComponent<PieceBehavior>().x = (int) move.secondPosition.Position.x;
                                current_piece.GetComponent<PieceBehavior>().y = (int) move.secondPosition.Position.y;
                                if (stateMachine.board[current_piece_script.x, current_piece_script.y] != null) {
                                    stateMachine.board[current_piece_script.x, current_piece_script.y].GetComponent<Rigidbody>().isKinematic = false;
                                    stateMachine.board[current_piece_script.x, current_piece_script.y].GetComponent<Rigidbody>().useGravity = false;
                                }
                                stateMachine.STATE = PLAYER_ONE_TRANSPERSPECTIVE_1;
                                target_pos_1 = new Vector3(target_tile.transform.position.x,
                                                player.transform.position.y,
                                                    target_tile.transform.position.z);
                                target_pos_2 = new Vector3(target_tile.transform.position.x,
                                                                current_piece.transform.position.y,
                                                                    target_tile.transform.position.z);
                                current_piece.GetComponent<AudioSource>().Play();
                                current_move = move;
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
                current_piece.GetComponent<MeshRenderer>().enabled = false;
                current_piece_script = current_piece.GetComponent<PieceBehavior>();
                player.transform.position = current_piece.transform.GetChild(0).position;
                if (Time.time > 10)
                {
                    text.GetComponent<UnityEngine.UI.Text>().font = gameFonts;
                    text.GetComponent<UnityEngine.UI.Text>().fontSize = 25;
                    text.GetComponent<UnityEngine.UI.Text>().color = clickColor;
                    if (start == false)
                    {
                        if (Time.time - textTime > 4)
                        {
                            text.GetComponent<UnityEngine.UI.Text>().text = texts[textCounter];
                            textCounter++;
                            textTime = Time.time;
                            if (textCounter > 8)
                            {
                                start = true;
                            }
                        }

                    }
                    else
                    {
                        //current_piece_script = current_piece.GetComponent<PieceBehavior>();
                        if (lightsCounter < 4)
                        {
                            if (Time.time - lightTime > 1)
                            {
                                string light_name1 = "Point light " + (lightsCounter * 4).ToString();
                                string light_name2 = "Point light " + (lightsCounter * 4 + 1).ToString();
                                string light_name3 = "Point light " + (lightsCounter * 4 + 2).ToString();
                                string light_name4 = "Point light " + (lightsCounter * 4 + 3).ToString();
                                GameObject.Find(light_name1).GetComponent<AudioSource>().Play();
                                GameObject.Find(light_name1).GetComponent<Light>().intensity = 3;
                                GameObject.Find(light_name2).GetComponent<Light>().intensity = 3;
                                GameObject.Find(light_name3).GetComponent<Light>().intensity = 3;
                                GameObject.Find(light_name4).GetComponent<Light>().intensity = 3;

                                if (light_name2 == "Point light 9")
                                {
                                    GameObject.Find("fire_sound").GetComponent<AudioSource>().Play();
                                }
                                lightTime = Time.time;
                                lightsCounter++;
                            }
                        }
                        else
                        {
                            // AI: SELECT THE STARTING PIECE AS KING
                            piece_selected = GameObject.Find(current_piece_script.x.ToString() + " " + current_piece_script.y.ToString()).GetComponent<Piece_new>();
                            piece_selected.Selected();
                            DrawTile(1);
                            stateMachine.STATE = PLAYER_ONE_SELECT;
                        }
                    }
                }
            }
            if (stateMachine.STATE == PLAYER_ONE_SELECT)
            {
                player.transform.position = current_piece.transform.GetChild(0).position;
            }
            if (stateMachine.STATE == PLAYER_ONE_TRANSPERSPECTIVE)
            {
                if (player.transform.position != target_piece.transform.GetChild(0).position)
                {
                    //player.transform.position = Vector3.MoveTowards(player.transform.position, target_piece.transform.position + Vector3.up, transperspective_speed * Time.deltaTime);
                    player.transform.position = target_piece.transform.GetChild(0).position;
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
                    //boardManager.makeMove(current_piece_script.x, current_piece_script.y);
                    piece_selected = GameObject.Find(current_piece_script.x.ToString() + " " + current_piece_script.y.ToString()).GetComponent<Piece_new>();
                    piece_selected.Selected();
                    DrawTile(1);
                }
            }
            if (stateMachine.STATE == PLAYER_ONE_MOVE)
            {

            }
            if (stateMachine.STATE == PLAYER_ONE_TRANSPERSPECTIVE_1)
            {
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
                    if (current_piece.name == "knight_white" || current_piece.name == "knight_white 1")
                    {
                        if (horse_rising)
                        {
                            if (player.transform.position.y > knight_height.y)
                            {
                                horse_rising = false;
                            }
                            current_piece.transform.position += 0.1f * Vector3.up;
                            player.transform.position += 0.1f * Vector3.up;
                        }
                        current_piece.GetComponent<AudioSource>().clip = horse_sound;
                    }
                    killTime = Time.time;
                }
                else
                {
                    horse_rising = true;
                    if (current_piece.GetComponent<AudioSource>().isPlaying)
                    {
                        current_piece.GetComponent<AudioSource>().Stop();
                    }

                    if (stateMachine.board[current_piece_script.x, current_piece_script.y] != null)
                    {
                        killing = true;
                        kill(stateMachine.board[current_piece_script.x, current_piece_script.y], true);
                    }

                    if (!killing)
                    {
                        DrawTile(0);

                        // AI: AI move
                        //enemy_move = boardManager.decideAndMoveEnemyPiece();\
                        if (stateMachine.STATE != END_GAME)
                        {
                            stateMachine.STATE = OPPONENT_MOVES;
                            //boardManager.makeMove(current_piece_script.x, current_piece_script.y);
                            Container container_script = GameObject.Find(current_piece_script.x.ToString() + current_piece_script.y.ToString()).GetComponent<Container>();
                            container_script.Selected();

                            update_board((int)current_move.firstPosition.Position.x,
                                            (int)current_move.firstPosition.Position.y,
                                            current_piece.GetComponent<PieceBehavior>().x,
                                            current_piece.GetComponent<PieceBehavior>().y);

                            System.Random rnd = new System.Random();
                            ab.maxDepth = rnd.Next(1, 4);
                            ai_move = ab.GetMove();
                            enemy_move[0] = (int)ai_move.firstPosition.Position.x;
                            enemy_move[1] = (int)ai_move.firstPosition.Position.y;
                            enemy_move[2] = (int)ai_move.secondPosition.Position.x;
                            enemy_move[3] = (int)ai_move.secondPosition.Position.y;
                            if (stateMachine.board[enemy_move[2], enemy_move[3]] != null && stateMachine.board[enemy_move[2], enemy_move[3]] != current_piece)
                            {
                                stateMachine.board[enemy_move[2], enemy_move[3]].GetComponent<Rigidbody>().isKinematic = false;
                                stateMachine.board[enemy_move[2], enemy_move[3]].GetComponent<Rigidbody>().useGravity = false;
                            }
                            gameManager.DoAIMove(ai_move);

                            enemypiece = stateMachine.board[enemy_move[0], enemy_move[1]];
                            enemy_target_tile = GameObject.Find(GetTileName(enemy_move[2], enemy_move[3]));

                            enemy_target_pos = new Vector3(enemy_target_tile.transform.position.x,
                                                            enemypiece.transform.position.y,
                                                            enemy_target_tile.transform.position.z);
                            killingMove = enemypiece.transform.position - enemy_target_pos;
                            killingMove.Normalize();
                            enemypiece.GetComponent<AudioSource>().Play();
                        }
                    }
                }
            }
            if (stateMachine.STATE == OPPONENT_MOVES)
            {
                
                if (enemypiece.transform.position != enemy_target_pos)
                {
                    enemypiece.transform.position = Vector3.MoveTowards(enemypiece.transform.position, enemy_target_pos, transperspective_speed * Time.deltaTime);

                    if (enemypiece.name == "knight_black" || enemypiece.name == "knight_black 1")
                    {
                        if (horse_rising)
                        {
                            if (enemypiece.transform.position.y > knight_height.y)
                            {
                                horse_rising = false;
                            }
                            enemypiece.transform.position += 0.2f * Vector3.up;
                        }
                        enemypiece.GetComponent<AudioSource>().clip = horse_sound;
                    }
                    killTime = Time.time;
                }
                else
                {
                    //Debug.Log("Enemy move completes!");
                    //boardManager.resetTiles();
                    //boardManager.makeMove(current_piece_script.x, current_piece_script.y);
                    horse_rising = true;
                    if (enemypiece.GetComponent<AudioSource>().isPlaying)
                    {
                        enemypiece.GetComponent<AudioSource>().Stop();
                    }

                    if (stateMachine.board[enemy_move[2], enemy_move[3]] != null)
                    {
                        killing = true;
                        kill(stateMachine.board[enemy_move[2], enemy_move[3]], false);
                    }
                    else
                    {
                        current_piece_script = current_piece.GetComponent<PieceBehavior>();
                        piece_selected.gameObject.name = current_piece_script.x.ToString() + " " + current_piece_script.y.ToString();
                        piece_selected.Selected();
                    }

                    if (!killing && stateMachine.STATE != END_GAME)
                    {
                        update_board((int)ai_move.firstPosition.Position.x, (int)ai_move.firstPosition.Position.y,
                                    (int)ai_move.secondPosition.Position.x, (int)ai_move.secondPosition.Position.y);
                        stateMachine.STATE = PLAYER_ONE_SELECT;
                        DrawTile(1);
                    }
                }
            }
            if (stateMachine.STATE == END_GAME)
            {
                if (lightsCounter > 0)
                {
                    if (Time.time - textTime > 1)
                    {
                        lightsCounter--;
                        string light_name1 = "Point light " + (lightsCounter * 4).ToString();
                        string light_name2 = "Point light " + (lightsCounter * 4 + 1).ToString();
                        string light_name3 = "Point light " + (lightsCounter * 4 + 2).ToString();
                        string light_name4 = "Point light " + (lightsCounter * 4 + 3).ToString();
                        GameObject.Find(light_name1).GetComponent<AudioSource>().Play();
                        GameObject.Find(light_name1).GetComponent<Light>().intensity = 0;
                        GameObject.Find(light_name2).GetComponent<Light>().intensity = 0;
                        GameObject.Find(light_name3).GetComponent<Light>().intensity = 0;
                        GameObject.Find(light_name4).GetComponent<Light>().intensity = 0;

                        if (light_name2 == "Point light 9")
                        {
                            GameObject.Find("fire_sound").GetComponent<AudioSource>().Stop();
                        }
                        textTime = Time.time;
                    }
                }
                else
                {
                    if (Time.time - textTime > 4 && textCounter < 2)
                    {
                        text.GetComponent<UnityEngine.UI.Text>().text = endText[textCounter];
                        textCounter++;
                        textTime = Time.time;
                    }
                }
            }
        }

        public string GetTileName(int x, int y)
        {
            string[] name_x = { "A", "B", "C", "D", "E", "F", "G", "H" };
            string[] name_y = { "8", "7", "6", "5", "4", "3", "2", "1" };
            return name_x[x] + name_y[y];
        }

        public void DrawTile(int matnumber)
        {
            //foreach (Position pos in boardManager.possibleMovePositions)
            //{
            //    string tileName = GetTileName(pos.x, pos.y);
            //    //Debug.Log(tileName);

            //    GameObject current_tile = GameObject.Find(tileName);
            //    MaterialControl current_matcontrol = current_tile.GetComponent<MaterialControl>();
            //    Material[] mat = { current_matcontrol.material_list[matnumber] };
            //    current_tile.GetComponent<MeshRenderer>().materials = mat;
            //}

            foreach(Move_new move in piece_selected.moves)
            {
                string tileName = GetTileName((int) move.secondPosition.Position.x, (int) move.secondPosition.Position.y);
                //Debug.Log("Possible move: " + tileName);

                GameObject current_tile = GameObject.Find(tileName);
                MaterialControl current_matcontrol = current_tile.GetComponent<MaterialControl>();
                Material[] mat = new Material[1];
                if (matnumber != 0 && move.pieceKilled != null)
                {
                    mat[0] = current_matcontrol.material_list[2];
                }
                else
                {
                    mat[0] = current_matcontrol.material_list[matnumber];
                }
                current_tile.GetComponent<MeshRenderer>().materials = mat;
            }
        }

        public void update_board(int x1, int y1, int x2, int y2)
        {
            stateMachine.board[x2, y2] = stateMachine.board[x1, y1];
            stateMachine.board[x1, y1] = null;
        }

        public void kill(GameObject killed_piece, bool is_enemy)
        {
            if (Time.time - killTime > 4)
            {
                killing = false;
                //Debug.Log("killed");

                if (killed_piece.name == "king_white")
                {
                    stateMachine.STATE = END_GAME;
                    Destroy(killed_piece);
                    Debug.Log("You lose");
                    endText[0] = "YOU LOST AND NOW IT IS YOUR TURN";
                    endText[1] = "TO BE TRAPPED HERE, IN THIS DARK DARK DUNGEON";
                    textCounter = 0;
                    textTime = Time.time;
                    return;
                }
                else if (killed_piece.name == "king_black")
                {
                    stateMachine.STATE = END_GAME;
                    Destroy(killed_piece);
                    Debug.Log("You won!");
                    endText[0] = "AHHH... I DON'T BELIEVE THIS!";
                    endText[1] = "THIS SHOULD NOT BE THE END OF VOLDEMORT!";
                    textTime = Time.time;
                    textCounter = 0;
                    return;
                }
                else
                {
                    if (is_enemy)
                    {
                        Destroy(killed_piece);
                    }
                    else
                    {
                        if (killed_piece == current_piece)
                        {
                            current_piece = GameObject.Find("king_white");
                            current_piece.GetComponent<MeshRenderer>().enabled = false;
                            current_piece_script = current_piece.GetComponent<PieceBehavior>();
                            piece_selected = GameObject.Find(current_piece_script.x.ToString() + " " + current_piece_script.y.ToString()).GetComponent<Piece_new>();
                            piece_selected.Selected();
                        }
                        else
                        {
                            piece_selected.gameObject.name = current_piece_script.x.ToString() + " " + current_piece_script.y.ToString();
                            piece_selected.Selected();
                        }
                        Destroy(killed_piece);
                    }
                }
            }
            else
            {
                //Debug.Log(Time.time - killTime);
                if (current_piece == killed_piece)
                {
                    //Debug.Log(killingMove);
                    
                    if (Time.time - killTime < 2.5)
                    {
                        player.transform.position += Vector3.up * 0.01f;
                        player.transform.position -= killingMove * 0.08f;
                    }

                    ParticleSystem system = player.GetComponentInChildren<ParticleSystem>();
                    if (!system.isPlaying)
                    {
                        system.Play();
                    }

                    current_piece.GetComponent<AudioSource>().clip = kill_sound;
                    if (!current_piece.GetComponent<AudioSource>().isPlaying)
                    {
                        current_piece.GetComponent<AudioSource>().Play();
                    }
                }
                else
                {
                    ParticleSystem system = killed_piece.GetComponent<ParticleSystem>();
                    if (!system.isPlaying)
                    {
                        system.Play();
                    }
                    if (Time.time - killTime > 2)
                    {
                        killed_piece.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
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