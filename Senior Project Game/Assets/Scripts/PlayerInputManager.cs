using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour {
    public KeyCode Menukey = KeyCode.Escape;
    [Header("Player Movement")]
    public KeyCode Forward = KeyCode.W;
    public KeyCode Backward = KeyCode.S;
    public KeyCode Left = KeyCode.A;
    public KeyCode Right = KeyCode.D;
    public KeyCode JumpKey = KeyCode.Space;
    public KeyCode WalkKey = KeyCode.LeftShift;
    public KeyCode SprintKey = KeyCode.LeftControl;

    public int VerticallMotion { get { return Input.GetKey(Backward) ? Input.GetKey(Forward) ? 0 : -1 : Input.GetKey(Forward) ? 1 : 0; } }
    public int HorizontalMotion { get { return Input.GetKey(Left) ? Input.GetKey(Right) ? 0 : -1 : Input.GetKey(Right) ? 1 : 0; } }
    public bool LateralInputExists {  get { return Input.GetKey(Forward) || Input.GetKey(Backward) || Input.GetKey(Left) || Input.GetKey(Right); } }
    public bool JumpKeyDown { get { return Input.GetKeyDown(JumpKey); } }
    public bool JumpKeyHeld { get { return Input.GetKey(JumpKey); } }
    public bool JumpKeyUp { get { return Input.GetKeyUp(JumpKey); } }
    public bool Walk { get { return Input.GetKey(WalkKey); } }
    public bool Sprint { get { return Input.GetKey(SprintKey); } }
    public bool MenuKeyDown {  get { return Input.GetKey(Menukey); } }
}
