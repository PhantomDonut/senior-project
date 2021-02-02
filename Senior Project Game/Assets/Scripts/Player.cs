using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour {

    [Header("Movement Settings")]
    public float speed = 6;
    [SerializeField] private float walkSpeed = 3;
    [SerializeField] private float runSpeed = 6;
    [SerializeField] private float sprintSpeed = 9;
    private Vector2 input;
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    private bool justJumped = false;
    public int maxJumps = 1;
    private int jumpsRemaining;
    bool walking;
    bool running;
    bool sprinting;
    private PlayerInputManager inputManager;
    private CollisionInfo collisionInfo;
    private PlayerController playerController;
    new Rigidbody rigidbody;

    [Header("Surface")]
    public bool onPlatform;
    private Transform platformTransform;

    [Header("Visuals")]
    public Transform visual;
    private Animator animator;
    CameraController playerCamera;
    float angle;
    public float visualTurnSpeed = 10;
    Quaternion targetRotation;

    [Header("Interaction")]
    public bool justTeleported;

    [Header("Debug")]
    public bool debug;
    public TextMeshProUGUI jumpCounterText;

    private void Start() {
        inputManager = GetComponent<PlayerInputManager>();
        playerCamera = Camera.main.GetComponent<CameraController>();

        playerController = GetComponent<PlayerController>();
        playerController.gravity = new Vector3(0, -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2), 0);
        playerController.maxJumpVelocity = Mathf.Abs(playerController.gravity.y) * timeToJumpApex;
        playerController.minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(playerController.gravity.y) * minJumpHeight);

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.centerOfMass = new Vector3(0, 0, 0);
        rigidbody.inertiaTensor = new Vector3(1, 1, 1);

        animator = visual.GetComponentInChildren<Animator>();
    }

    private void Update() {
        input = new Vector2(inputManager.HorizontalMotion, inputManager.VerticallMotion);

        running = !inputManager.Walk;
        sprinting = running && inputManager.Sprint;
        speed = running ? sprinting ? sprintSpeed : runSpeed : walkSpeed;

        bool validJump = false;
        if (inputManager.JumpKeyDown) {
            if ((collisionInfo.grounded || jumpsRemaining > 0) && !justJumped) {
                jumpsRemaining--;
                StartCoroutine(JumpReset());
                validJump = true;
            }
        }

        if (!collisionInfo.groundedLastFrame && collisionInfo.grounded) {
            jumpsRemaining = collisionInfo.grounded ? (collisionInfo.sliding || collisionInfo.slidingLastFrame) ? 1 : maxJumps : jumpsRemaining;
        }
        jumpCounterText.text = string.Format("Jumps: {0}", jumpsRemaining);

        if(onPlatform && !justJumped) {
            transform.localPosition = new Vector3(transform.localPosition.x, ((platformTransform.GetComponent<BoxCollider>().size.y * platformTransform.localScale.y) * 0.5f + 0.5f) / platformTransform.localScale.y, transform.localPosition.z);
        }

        collisionInfo = playerController.CalculateFrameVelocity(input, speed, validJump, inputManager.JumpKeyUp, justJumped);
        
        bool horizontalInputExists = (input.x != 0 || input.y != 0);
        Quaternion savedRotation = visual.rotation;
        if (horizontalInputExists) {
            CalculateDirection();
        }

        AnimatePlayer(horizontalInputExists, savedRotation);

        rigidbody.velocity = collisionInfo.velocity;
        if (debug) DrawDebugLines();
    }

    private void CalculateDirection() {
        angle = Mathf.Atan2(input.x, input.y);
        angle *= Mathf.Rad2Deg;
        angle += playerCamera.sharpRotationAngle;
        targetRotation = Quaternion.Euler(0, angle, 0);
        transform.rotation = targetRotation;
    }

    private void AnimatePlayer(bool horizontalInputExists, Quaternion savedRotation) {
        animator.SetBool("Grounded", collisionInfo.grounded);
        animator.SetBool("Walking", horizontalInputExists & !running);
        animator.SetBool("Running", horizontalInputExists & running & !sprinting);
        animator.SetBool("Sprinting", horizontalInputExists & sprinting);
        animator.SetBool("Sliding", collisionInfo.sliding);
        if (!collisionInfo.groundedLastFrame && collisionInfo.grounded) animator.SetTrigger("Landing");
        
        float velocityAngle = Mathf.Atan2(collisionInfo.velocity.x, collisionInfo.velocity.z) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0, velocityAngle, 0); 
        float lerpTime = Mathf.Clamp(Mathf.InverseLerp(0, 2, Mathf.Abs(collisionInfo.velocity.x)) + Mathf.InverseLerp(0, 2, Mathf.Abs(collisionInfo.velocity.z)), 0, 1);
        visual.rotation = Quaternion.Slerp(savedRotation, targetRotation, Time.deltaTime * visualTurnSpeed * lerpTime);
    }

    IEnumerator JumpReset() {
        justJumped = true;
        yield return new WaitForSeconds(0.2f);
        justJumped = false;
    }

    public void PlatformAttatchmentToggle(bool state, Transform platformOrNull) {
        transform.SetParent(platformOrNull);
        onPlatform = state;
        platformTransform = platformOrNull;
        if (state) {
            rigidbody.interpolation = RigidbodyInterpolation.None;
        } else {
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
    
    void DrawDebugLines() {
        Debug.DrawLine(transform.position, transform.position + collisionInfo.forward * 0.5f * 2, Color.blue);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * 0.5f, Color.green);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * 0.5f * 2f, Color.red);
    }

    public IEnumerator JustTeleported() {
        justTeleported = true;
        yield return new WaitForSeconds(Teleporter.TELEPORT_COOLDOWN);
        justTeleported = false;
    }

    public void CancelMomentum() {
        collisionInfo.velocity = Vector3.zero;
        collisionInfo.velocityPriorFrame = Vector3.zero;
    }
}
