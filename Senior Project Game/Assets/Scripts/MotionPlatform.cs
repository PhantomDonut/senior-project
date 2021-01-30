using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MotionPlatform : Surface {

    public bool motionPlatform;
    [ShowIf("motionPlatform", true)] public Vector3 movementOffset;
    [ShowIf("motionPlatform", true)] Vector3 startingPosition;
    [ShowIf("motionPlatform", true)] public float movementSpeed;
    private float trueMovementSpeed;
    [ShowIf("motionPlatform", true)] public bool lockedUntilPlayer;
    private bool motionLockedState = true;
    private float startTime;
    [HideInInspector] public bool directionUp;

    void Start() {
        base.Start();
        startingPosition = transform.position;
        float units = Vector3.Distance(startingPosition, startingPosition + movementOffset);
        trueMovementSpeed = movementSpeed / units;
    }

    private void Update() {
        if(motionPlatform && (!lockedUntilPlayer || (lockedUntilPlayer && !motionLockedState))) {
            float time = lockedUntilPlayer ? (GameManager.GameTime - startTime) * trueMovementSpeed : GameManager.GameTime * trueMovementSpeed;
            directionUp = (time % 2) < 1;
            transform.position = Vector3.Lerp(startingPosition, startingPosition + movementOffset, Mathf.PingPong(time, 1));
        }
    }

    public void UnlockPlatform() {
        if(lockedUntilPlayer && motionLockedState) {
            motionLockedState = false;
            startTime = GameManager.GamePhysicsTime;
        }
    }
}
