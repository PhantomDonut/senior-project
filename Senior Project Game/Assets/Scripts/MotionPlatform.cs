using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MotionPlatform : Surface {

    public bool enableMotion;
    [ShowIf("enableMotion", true)] public Vector3 movementOffset;
    [ShowIf("enableMotion", true)] Vector3 startingPosition;
    [ShowIf("enableMotion", true)] [Range(0.5f, 6)] public float movementSpeed;
    private float trueMovementSpeed;
    public bool enableRotation;
    [ShowIf("enableRotation", true)] public float rotationSpeed;
    private float trueRotationSpeed;
    [ShowIf("enableMotion", true)] public bool lockedUntilPlayer;
    private bool motionLockedState = true;
    private float startTime;
    [SerializeField] private bool generateTriggers = true;
    [HideInInspector] public bool directionUp;

    new void Start() {
        base.Start();
        if(generateTriggers) new GameObject("Motion").AddComponent<SurfaceTrigger>().Create(this, SurfaceTriggerType.Motion);
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
            trueRotationSpeed = 360 / rotationSpeed;
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
            transform.rotation = Quaternion.Euler(transform.rotation.x, (time * trueRotationSpeed) % 360, transform.rotation.z);
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
