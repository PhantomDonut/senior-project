using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MotionPlatform : Surface {

    public bool enableMotion;
    [ShowIf("enableMotion", true)] public Vector3 movementOffset;
    [ShowIf("enableMotion", true)] Vector3 startingPosition;
    [ShowIf("enableMotion", true)] public float movementSpeed;
    private float trueMovementSpeed;
    [ShowIf("enableMotion", true)] public bool lockedUntilPlayer;
    private bool motionLockedState = true;
    private float startTime;
    [HideInInspector] public bool directionUp;

    new void Start() {
        base.Start();
        new GameObject("Motion").AddComponent<SurfaceTrigger>().Create(this, SurfaceTriggerType.Motion);
        startingPosition = transform.position;
        float units = Vector3.Distance(startingPosition, startingPosition + movementOffset);
        trueMovementSpeed = movementSpeed / units;
    }

    private void Update() {
        if(enableMotion && (!lockedUntilPlayer || (lockedUntilPlayer && !motionLockedState))) {
            float time = lockedUntilPlayer ? (GameManager.GameTime - startTime) * trueMovementSpeed : GameManager.GameTime * trueMovementSpeed;
            directionUp = (time % 2) < 1;
            transform.position = Vector3.Lerp(startingPosition, startingPosition + movementOffset, Mathf.PingPong(time, 1));
        }
    }

    public void PlayerPlatformToggle(bool state) {
        if (lockedUntilPlayer && motionLockedState && state) {
            motionLockedState = false;
            startTime = GameManager.GamePhysicsTime;
        }
        GameManager.Instance.player.PlatformAttatchmentToggle(state, state ? transform : null);
    }
}
