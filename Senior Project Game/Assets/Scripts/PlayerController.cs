﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour {
    [Header("Collision")]
    public LayerMask ground;
    public float heightPadding = 0.05f;
    Bounds bounds;
    float height = 1;
    RaycastHit hitInfo;
    private CollisionInfo collisionInfo;

    Vector3 movementVelocity;
    Vector3 verticalVelocity;

    Vector3 movementTotal;

    [Header("Slopes")]
    [Range(0, 90)] public float maxGroundAngle = 50;
    float currentSlideAcceleration;
    Vector3 slideVelocity;

    [HideInInspector] public Vector3 gravity;
    [HideInInspector] public float maxJumpVelocity;
    [HideInInspector] public float minJumpVelocity;

    [Header("Surface")]
    public LayerMask translucentLayers;
    [SerializeField] [Range(0, 1)] private float friction = 1;
    private Surface lastFrameSurface;

    [Header("Debug")]
    public TextMeshProUGUI debugText;
    private const string debugTextFormat = "Velocity: {3}\nGrounded: {0}\nSliding: {1} & {2} @ {5}\nVertical Velocity: {4}";

    private void Start() {
        bounds = GetComponent<CapsuleCollider>().bounds;
        height = 0.5f;
    }
    public CollisionInfo CalculateFrameVelocity(Vector2 input, float speed, bool validJump, bool jumpKeyUp, bool justJumped) {
        collisionInfo.velocityPriorFrame = collisionInfo.velocity;
        collisionInfo.slidingLastFrame = collisionInfo.sliding;

        //Value decays
        if (collisionInfo.sliding) {
            currentSlideAcceleration *= 1.15f;
            currentSlideAcceleration = Mathf.Clamp(currentSlideAcceleration, 0, 15);
        }
        else if (collisionInfo.grounded) {
            currentSlideAcceleration = 0.5f;
        }

        //Airborne velocity decay
        verticalVelocity.x *= 0.975f;
        verticalVelocity.z *= 0.975f;
        
        CalculateForward();
        CheckGround(justJumped);
        CalculateGroundAngle();

        if (validJump) {
            verticalVelocity.y = maxJumpVelocity;
            if (collisionInfo.sliding) {
                verticalVelocity.x += slideVelocity.normalized.x * currentSlideAcceleration * 0.9f;
                verticalVelocity.z += slideVelocity.normalized.z * currentSlideAcceleration * 0.9f;
            } else {
                verticalVelocity += movementVelocity * 0.5f;
            }
        }

        if (jumpKeyUp) {
            if (verticalVelocity.y > minJumpVelocity) {
                verticalVelocity.y = minJumpVelocity;
            }
        }

        ApplyGravity();

        movementVelocity = Vector3.zero;
        if(Mathf.Abs(input.x) + Mathf.Abs(input.y) > 0) {
            Move(speed);
        }

        movementTotal = movementVelocity + slideVelocity;

        Vector3 frameVelocity = movementTotal + verticalVelocity;
        frameVelocity = new Vector3(Mathf.Lerp(collisionInfo.velocityPriorFrame.x, frameVelocity.x, friction), frameVelocity.y, Mathf.Lerp(collisionInfo.velocityPriorFrame.z, frameVelocity.z, friction));
        collisionInfo.velocity = frameVelocity;
        return collisionInfo;
    }
    void CalculateForward() {
        if (!collisionInfo.grounded) {
            collisionInfo.forward = transform.forward;
            return;
        }

        collisionInfo.forward = Vector3.Cross(transform.right, hitInfo.normal);
    }
    void CheckGround(bool justJumped) {
        collisionInfo.groundedLastFrame = collisionInfo.grounded;
        friction = 1;

        if (Physics.BoxCast(transform.position, new Vector3(bounds.extents.x, 0.0125f, bounds.extents.z), -Vector3.up, out hitInfo, transform.rotation, height + heightPadding, ground) && !justJumped) {
            if (Vector3.Distance(transform.position, hitInfo.point) < height) {
                //Keep position leveled to the ground to prevent clipping
                transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * height, 5 * Time.deltaTime);
            }
            if (hitInfo.transform.gameObject.GetComponent<Surface>() != null) friction = hitInfo.transform.gameObject.GetComponent<Surface>().friction;
            collisionInfo.grounded = true;
        }
        else {
            collisionInfo.grounded = false;
        }
        //Detect passthrough platform
        RaycastHit passthroughBoxcast;
        Surface currentSurface = null;
        if (Physics.BoxCast(transform.position, new Vector3(bounds.extents.x, 0.0125f, bounds.extents.z), -Vector3.up, out passthroughBoxcast, transform.rotation, height + heightPadding, translucentLayers)) {
            if(passthroughBoxcast.transform != null) {
                currentSurface = passthroughBoxcast.transform.GetComponent<Surface>();
                currentSurface.PlayerAbove(true);
            }
        }

        if (currentSurface != lastFrameSurface && lastFrameSurface != null) lastFrameSurface.PlayerAbove(false);
        lastFrameSurface = currentSurface;
        
    }

    void CalculateGroundAngle() {
        if (!collisionInfo.grounded) {
            collisionInfo.groundAngle = 0;
            return;
        }
        collisionInfo.groundAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
    }

    void ApplyGravity() {
        if (!collisionInfo.grounded) {
            verticalVelocity += gravity * Time.deltaTime;
        } else {
            verticalVelocity = Vector3.zero;
        }

        collisionInfo.sliding = collisionInfo.groundAngle > maxGroundAngle;
        if (collisionInfo.sliding) {
            slideVelocity = Vector3.Cross(Vector3.Cross(Vector3.up, hitInfo.normal), hitInfo.normal) * currentSlideAcceleration;
            //Debug.Log("Slide Angle is " + Vector3.Cross(Vector3.Cross(Vector3.up, hitInfo.normal), hitInfo.normal));
        } else {
            slideVelocity = Vector3.zero;
        }
    }

    private void Move(float speed) {
        float speedModifier = 1;
        if (collisionInfo.groundAngle >= maxGroundAngle) speedModifier = 0.25f;
        if (!collisionInfo.grounded) speedModifier = 0.75f;
        movementVelocity = collisionInfo.forward * speed * speedModifier;
    }

    private void LateUpdate() {
        debugText.text = string.Format(debugTextFormat, collisionInfo.grounded, collisionInfo.sliding, slideVelocity, movementTotal, verticalVelocity, System.Math.Round(currentSlideAcceleration, 2));
    }
}

public struct CollisionInfo {
    public bool above, grounded;
    public bool groundedLastFrame;
    public bool front, back;
    public bool left, right;

    public bool sliding;
    public bool slidingLastFrame;
    public float groundAngle;
    public Vector3 velocity;
    public Vector3 velocityPriorFrame;
    public Vector3 forward;
}