using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour {

    PlayerUI playerUI;

    public KeyCode Menukey = KeyCode.Escape;
    public KeyCode RewindKey = KeyCode.R;
    [Header("Player Movement")]
    public KeyCode Forward = KeyCode.W;
    public KeyCode Backward = KeyCode.S;
    public KeyCode Left = KeyCode.A;
    public KeyCode Right = KeyCode.D;
    public KeyCode JumpKey = KeyCode.Space;
    public KeyCode SprintKey = KeyCode.LeftShift;
    [Header("Ability")]
    public KeyCode FreeplayColor = KeyCode.X;

    public int VerticallMotion { get { return Input.GetKey(Backward) ? Input.GetKey(Forward) ? 0 : -1 : Input.GetKey(Forward) ? 1 : 0; } }
    public int HorizontalMotion { get { return Input.GetKey(Left) ? Input.GetKey(Right) ? 0 : -1 : Input.GetKey(Right) ? 1 : 0; } }
    public bool JumpKeyDown { get { return Input.GetKeyDown(JumpKey); } }
    public bool JumpKeyHeld { get { return Input.GetKey(JumpKey); } }
    public bool JumpKeyUp { get { return Input.GetKeyUp(JumpKey); } }
    public bool Sprint { get { return Input.GetKey(SprintKey); } }

    void Start() {
        playerUI = FindObjectOfType<PlayerUI>();
    }

    void Update() {
        if (Input.GetKeyDown(Menukey)) {
            playerUI.MenuPressed();
        }
        if(Input.GetKeyDown(RewindKey)) {
            GameManager.Instance.RewindToggle();
        }

    }
}
