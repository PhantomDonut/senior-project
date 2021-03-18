using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MotionPlatform : Surface {

    public bool enableMotion;
    public bool doubleSidedColliders;
    [ShowIf("enableMotion", true)] public Vector3 movementOffset;
    [ShowIf("enableMotion", true)] Vector3 startingPosition;
    [ShowIf("enableMotion", true)] [Range(0.5f, 6)] public float movementSpeed;
    private float trueMovementSpeed;
    public bool enableRotation;
    [ShowIf("enableRotation", true)] public Vector3 rotationSpeed;
    private Vector3 trueRotationSpeed;
    [ShowIf("enableMotion", true)] public bool lockedUntilPlayer;
    private bool motionLockedState = true;
    private float startTime;
    [SerializeField] private bool generateTriggers = true;
    [HideInInspector] public bool directionUp;

    new void Start() {
        base.Start();
        if (generateTriggers) {
            new GameObject("Motion").AddComponent<SurfaceTrigger>().Create(this, SurfaceTriggerType.Motion);
            if(doubleSidedColliders) new GameObject("Motion Double Side").AddComponent<SurfaceTrigger>().Create(this, SurfaceTriggerType.Motion, true);
        }
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.inertiaTensor = Vector3.one;

        if (enableMotion) {
            startingPosition = transform.position;
            float units = Vector3.Distance(startingPosition, startingPosition + movementOffset);
            trueMovementSpeed = movementSpeed / units;
        }
        if(enableRotation) {
            trueRotationSpeed = new Vector3(rotationSpeed.x != 0 ? 360 / rotationSpeed.x : 0, 
                rotationSpeed.y != 0 ? 360 / rotationSpeed.y : 0, 
                rotationSpeed.z != 0 ? 360 / rotationSpeed.z : 0);
        }
    }

    private void FixedUpdate() {
        if(enableMotion && (!lockedUntilPlayer || (lockedUntilPlayer && !motionLockedState))) {
            float time = lockedUntilPlayer ? (GameManager.GamePhysicsTime - startTime) * trueMovementSpeed : GameManager.GamePhysicsTime * trueMovementSpeed;
            directionUp = (time % 2) < 1;
            transform.position = Vector3.Lerp(startingPosition, startingPosition + movementOffset, Mathf.PingPong(time, 1));

        }
        if(enableRotation && (!lockedUntilPlayer || (lockedUntilPlayer && !motionLockedState))) {
            float time = lockedUntilPlayer ? (GameManager.GamePhysicsTime - startTime) : GameManager.GamePhysicsTime;
            transform.rotation = Quaternion.Euler(trueRotationSpeed.x != 0 ? (time * trueRotationSpeed.x) % 360 : transform.rotation.x, 
                trueRotationSpeed.y != 0 ? (time * trueRotationSpeed.y) % 360 : transform.rotation.y,
                trueRotationSpeed.z != 0 ? (time * trueRotationSpeed.z) % 360 : transform.rotation.z);
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
