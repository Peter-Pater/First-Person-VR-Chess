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

        // player movement speed
        public float transperspective_speed = 3;
        public float move_speed = 1;

        // control states
        const int POINTER_IN = 1;
        const int POINTER_OUT = 2;
        const int POINTER_CLICK = 3;

        // global states
        const int PLAYER_ONE_SELECT = 1;
        const int PLAYER_ONE_MOVE = 2;
        const int PLAYER_ONE_TRANSPERSPECTIVE = 3;
        const int OPPONENT_MOVES = 4;

        GameObject current_piece;
        GameObject player;
        GameObject target_piece;
        StateMachine stateMachine;

        BoardManager boardManager;

        private void Start()
        {
            if (pose == null)
                pose = this.GetComponent<SteamVR_Behaviour_Pose>();
            if (pose == null)
                Debug.LogError("No SteamVR_Behaviour_Pose component found on this object");
            
            if (interactWithUI == null)
                Debug.LogError("No ui interaction action has been set on this component.");

            current_piece = GameObject.Find("king_white");
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
                Debug.Log("Current: " + hit.transform.name);
                //Debug.Log("Previous: " + previousContact.name);
                previousContact = hit.transform;
                LaserEventHandler(argsIn, POINTER_IN);
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
        }

        public void LaserEventHandler(PointerEventArgs element, int control_state)
        {
            PieceBehavior piecebehavior = element.target.gameObject.GetComponent<PieceBehavior>();

            if (piecebehavior && element.target.gameObject != current_piece)
            {
                // TO DO: PLAYER_ONE_MOVE SHOULD NOT BE HERE!
                if (stateMachine.STATE == PLAYER_ONE_MOVE || stateMachine.STATE == PLAYER_ONE_SELECT)
                {
                    piecebehavior.control_state = control_state;
                    if (control_state == POINTER_CLICK)
                    {
                        target_piece = element.target.gameObject;
                    }
                }
            }
        }

        public void PerspectiveHandler()
        {
            // TO DO: RECONSIDER THIS STATE
            if (stateMachine.STATE != PLAYER_ONE_TRANSPERSPECTIVE && stateMachine.STATE != PLAYER_ONE_MOVE)
            {
                player.transform.position = current_piece.transform.position + Vector3.up;
            }
            else
            {
                if (stateMachine.STATE == PLAYER_ONE_TRANSPERSPECTIVE)
                {
                    if (player.transform.position != target_piece.transform.position + Vector3.up)
                    {
                        player.transform.position = Vector3.MoveTowards(player.transform.position, target_piece.transform.position + Vector3.up, transperspective_speed * Time.deltaTime);
                    }
                    else
                    {
                        Debug.Log("Transperspective complete!");
                        stateMachine.STATE = PLAYER_ONE_MOVE;
                        //stateMachine.STATE = PLAYER_ONE_SELECT;
                        current_piece.GetComponent<MeshRenderer>().enabled = true;
                        current_piece.GetComponent<Collider>().enabled = true;
                        current_piece = target_piece;
                    }
                }
                if (stateMachine.STATE == PLAYER_ONE_MOVE)
                {

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