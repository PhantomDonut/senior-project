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
    new Rigidbody rigidbody;

    Vector3 movementVelocity;
    Vector3 verticalVelocity;
    private const int TERMINAL_VELOCITY = 30;
    private Vector3 terminalVertical;

    Vector3 movementTotal;

    [HideInInspector] public Vector3 wallslideSpeed;
    [HideInInspector] public float walljumpPower;
    [HideInInspector] public float walljumpHeightMultiplier;

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
    private float globalFrictionMultiplier = 1;
    private Surface hitSurface;
    private Surface lastFrameSurface;
    [SerializeField] private float bounceMultiplier = 0;
    private bool justBounced = false;
    private const int BOUNCE_MINIMUM = 6;

    private void Start() {
        rigidbody = GetComponent<Rigidbody>();
        bounds = GetComponent<CapsuleCollider>().bounds;
        height = 0.5f;
        terminalVertical = new Vector3(0, -TERMINAL_VELOCITY, 0);
    }
    public CollisionInfo CalculateFrameVelocity(Vector2 input, float speed, bool validJump, bool jumpKeyUp, bool justJumped, bool onWall, Vector3 wallNormal, float movementModifier, bool spinFrame) {
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
            } else if (onWall) {
                verticalVelocity.x = wallNormal.x * walljumpPower;
                verticalVelocity.z = wallNormal.z * walljumpPower;
                verticalVelocity.y *= walljumpHeightMultiplier;
            } else {
                verticalVelocity += movementVelocity * 0.5f;
            }
        }

        if (jumpKeyUp) {
            if (verticalVelocity.y > minJumpVelocity) {
                verticalVelocity.y = minJumpVelocity;
            }
        }

        ApplyGravity(onWall);

        if (spinFrame && !collisionInfo.grounded) {
            verticalVelocity.x += movementVelocity.x * 0.75f;
            verticalVelocity.z += movementVelocity.z * 0.75f;
            verticalVelocity.y = Mathf.Max(5, verticalVelocity.y);
        }

        movementVelocity = Vector3.zero;
        if(Mathf.Abs(input.x) + Mathf.Abs(input.y) > 0) {
            Move(speed);
        }

        movementTotal = movementVelocity + slideVelocity;
        movementTotal *= movementModifier;
        Vector3 frameVelocity = movementTotal + verticalVelocity;
        if (collisionInfo.slidingLastFrame && !collisionInfo.sliding && collisionInfo.grounded) StartCoroutine(SlideExitFriction());
        friction /= globalFrictionMultiplier;
        frameVelocity = new Vector3(Mathf.Lerp(collisionInfo.velocityPriorFrame.x, frameVelocity.x, friction), frameVelocity.y, Mathf.Lerp(collisionInfo.velocityPriorFrame.z, frameVelocity.z, friction));
        collisionInfo.velocity = frameVelocity;
        if(GameManager.RequestDebug) GameManager.Instance.LoadDebugArgs(hitSurface != null ? hitSurface.surfaceAttributes.name : "None", hitSurface != null ? System.Convert.ToString(hitSurface.surfaceAttributes.friction) : "n/a", hitSurface != null ? System.Convert.ToString((int)hitSurface.surfaceAttributes.bounceMultiplier) : "n/a", verticalVelocity, slideVelocity, currentSlideAcceleration, wallslideSpeed.y);
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
        bounceMultiplier = 0;

        hitSurface = null;
        if (Physics.BoxCast(transform.position, new Vector3(bounds.extents.x, 0.0125f, bounds.extents.z), -Vector3.up, out hitInfo, transform.rotation, height + heightPadding, ground) && !justJumped) {
            if (Vector3.Distance(transform.position, hitInfo.point) < height) {
                //Keep position leveled to the ground to prevent clipping
                transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * height, 5 * Time.fixedDeltaTime);
            }
            hitSurface = hitInfo.transform.gameObject.GetComponent<Surface>();
            if (hitSurface != null) {
                friction = hitSurface.surfaceAttributes.friction;
                bounceMultiplier = (int)hitSurface.surfaceAttributes.bounceMultiplier * 0.1f;
                //Debug.Log(hitSurface.transform.name);
            }
            collisionInfo.grounded = true;
        }
        else {
            collisionInfo.grounded = false;
        }

        //Debug.Log(hitSurface != null ? hitSurface.name : "Null");
        if (hitSurface != lastFrameSurface && lastFrameSurface != null) {
            //lastFrameSurface.PlayerAbove(false);
        }
        lastFrameSurface = hitSurface;
    }

    void CalculateGroundAngle() {
        if (!collisionInfo.grounded) {
            collisionInfo.groundAngle = 0;
            return;
        }
        collisionInfo.groundAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
    }

    void ApplyGravity(bool onWall) {
        if (!collisionInfo.grounded && !onWall) {
            verticalVelocity += gravity * Time.fixedDeltaTime;
            if (verticalVelocity.y < -TERMINAL_VELOCITY) {
                verticalVelocity = terminalVertical;
            }
        } else if (onWall) {
            verticalVelocity += wallslideSpeed * Time.fixedDeltaTime;
        } else if (!justBounced) {
            verticalVelocity = Vector3.zero;
        }
        

        if(!collisionInfo.groundedLastFrame && collisionInfo.grounded && bounceMultiplier > 0 && Mathf.Abs(collisionInfo.velocityPriorFrame.y) > BOUNCE_MINIMUM) {
            Debug.Log("Downward was: " + collisionInfo.velocityPriorFrame.y + " upwards is " + Mathf.Abs(collisionInfo.velocityPriorFrame.y * bounceMultiplier));
            verticalVelocity += new Vector3(0, Mathf.Abs(collisionInfo.velocityPriorFrame.y * bounceMultiplier), 0);
            StartCoroutine(BounceDelay());

        }

        collisionInfo.sliding = collisionInfo.groundAngle > maxGroundAngle;
        if (collisionInfo.sliding) {
            slideVelocity = Vector3.Cross(Vector3.Cross(Vector3.up, hitInfo.normal), hitInfo.normal) * currentSlideAcceleration;
            //Debug.Log("Slide Angle is " + Vector3.Cross(Vector3.Cross(Vector3.up, hitInfo.normal), hitInfo.normal));
        } else {
            slideVelocity = Vector3.zero;
        }
    }

    IEnumerator SlideExitFriction() {
        globalFrictionMultiplier = 20;
        yield return new WaitForSeconds(0.33f);
        globalFrictionMultiplier = 1;
    }

    IEnumerator BounceDelay() {
        justBounced = true;
        yield return new WaitForSeconds(0.1f);
        justBounced = false;
    }

    private void Move(float speed) {
        float speedModifier = 1;
        if (collisionInfo.groundAngle >= maxGroundAngle) speedModifier = 0.25f;
        if (!collisionInfo.grounded) speedModifier = 0.75f;
        movementVelocity = collisionInfo.forward * speed * speedModifier;
    }

    private void LateUpdate() {
        //debugText.text = string.Format(debugTextFormat, collisionInfo.grounded, collisionInfo.sliding, slideVelocity, movementTotal, verticalVelocity, System.Math.Round(currentSlideAcceleration, 2));
    }

    public void CancelMomentum() {
        movementVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        slideVelocity = Vector3.zero;
        collisionInfo.velocity = Vector3.zero;
        collisionInfo.velocityPriorFrame = Vector3.zero;
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