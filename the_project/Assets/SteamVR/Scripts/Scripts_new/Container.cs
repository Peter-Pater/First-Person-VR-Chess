using UnityEngine;
using System.Collections;

public class Container : MonoBehaviour
{
    public Move_new move;
    GameManager manager;

    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    public void Selected()
    {
        manager.SwapPieces(move);
    }
}
