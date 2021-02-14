using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#pragma warning disable 0649
public class Player : MonoBehaviour {

    [Header("Movement Settings")]
    public float speed = 6;
    [SerializeField] private float walkSpeed = 3;
    [SerializeField] private float runSpeed = 6;
    [SerializeField] private float sprintSpeed = 9;
    private Vector2 input;
    bool walking;
    bool running;
    bool sprinting;
    private PlayerInputManager inputManager;
    private CollisionInfo collisionInfo;
    private PlayerController playerController;
    new Rigidbody rigidbody;
    private float globalMovementModifier = 1;

    [Header("Jumping")]
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    private bool justJumped = false;
    public int baseJumps = 1;
    public int extraJumps = 0;
    private int jumpsRemaining;
    const float LANDING_ANIM_TIME = 0.5f;
    [SerializeField] private AnimationCurve landingYScale;

    private bool jumpQueued;
    private float jumpQueueTime;
    const float JUMP_QUEUE_MAX = 0.1f;

    [Header("Wall Jump")]
    [SerializeField] [Range(0, 20)] private float walljumpPower = 8;
    [SerializeField] [Range(0, 1)] private float walljumpHeightMultiplier = 1;
    [SerializeField] private float wallSlideSpeed = 5;
    private bool onWall;
    private Vector3 wallNormal;
    const float WALLJUMP_INPUT_DELAY = 99;
    private float wallTime;
    private GameObject lastWallUsed;
    const float CONTROL_POST_WALLJUMP = 0.5f;

    [Header("Surface")]
    public bool onPlatform;
    private Transform platformTransform;
    private float holdingWallStartTime;

    [Header("Visuals")]
    public Transform visual;
    private Animator animator;
    CameraController playerCamera;
    float angle;
    public float visualTurnSpeed = 10;
    Quaternion targetRotation;
    public float visualMoveSpeed = 15;
    [SerializeField] private Cloth cloth;

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
        playerController.wallslideSpeed = new Vector3(0, -wallSlideSpeed, 0);
        playerController.walljumpPower = walljumpPower;
        playerController.walljumpHeightMultiplier = walljumpHeightMultiplier;

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.centerOfMass = new Vector3(0, 1, 0);
        rigidbody.inertiaTensor = new Vector3(1, 1, 1);

        animator = visual.GetComponentInChildren<Animator>();
    }

    private void Update() {
        input = new Vector2(inputManager.HorizontalMotion, inputManager.VerticallMotion);

        if (onWall) {
            wallTime = GameManager.GameTime - holdingWallStartTime;
            if (wallTime < WALLJUMP_INPUT_DELAY) {
                input = Vector2.zero;
            }
            if (collisionInfo.grounded) {
                StartCoroutine(ExitWalljump(false));
            }
        }

        Quaternion savedRotation = visual.rotation;
        if (inputManager.LateralInputExists && !onWall) {
            CalculateDirection();
        }

        if(transform.lossyScale != Vector3.one) {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1 / transform.lossyScale.z);
        }

        if(inputManager.JumpKeyDown) {
            jumpQueued = true;
            jumpQueueTime = GameManager.GameTime;
        } else if(jumpQueued && GameManager.GameTime - jumpQueueTime > JUMP_QUEUE_MAX) {
            jumpQueued = false;
        }

        if(Input.GetKeyDown(KeyCode.R)) {
            cloth.enabled = false;
            cloth.enabled = true;
        }

        jumpCounterText.text = string.Format("Jumps: {0} Queued: {1}", jumpsRemaining, jumpQueued);

        AnimatePlayer(inputManager.LateralInputExists, savedRotation);
    }

    private void FixedUpdate() {
        running = !inputManager.Walk;
        sprinting = running && inputManager.Sprint;
        speed = running ? sprinting ? sprintSpeed : runSpeed : walkSpeed;

        bool validJump = false;
        if (jumpQueued && (collisionInfo.grounded || jumpsRemaining > 0 || onWall) && !justJumped) {
            jumpsRemaining--;
            jumpQueued = false;
            StartCoroutine(JumpReset());
            validJump = true;
        }

        if (collisionInfo.groundedLastFrame && !collisionInfo.grounded) jumpsRemaining = extraJumps;
        if (!collisionInfo.groundedLastFrame && collisionInfo.grounded) StartCoroutine(Landing());

        collisionInfo = playerController.CalculateFrameVelocity(input, speed, validJump, inputManager.JumpKeyUp, justJumped, onWall, wallNormal, globalMovementModifier);

        rigidbody.velocity = collisionInfo.velocity;

        if (onPlatform && !justJumped) {
            transform.localPosition = new Vector3(transform.localPosition.x, ((platformTransform.GetComponent<BoxCollider>().size.y * platformTransform.localScale.y) * 0.5f + 0.5f) / platformTransform.localScale.y, transform.localPosition.z);
        }

        if(onWall && validJump) {
            globalMovementModifier = 0.2f;
            StartCoroutine(ExitWalljump(true));
        }

        if (debug) DrawDebugLines();
    }

    private void LateUpdate() {
        Vector3 targetPosition = transform.position;
        visual.transform.position = Vector3.Lerp(visual.transform.position, targetPosition, visualMoveSpeed * Time.deltaTime);
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
        
        float velocityAngle = Mathf.Atan2(collisionInfo.velocity.x, collisionInfo.velocity.z) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0, !onWall ? horizontalInputExists ? angle : velocityAngle : 180 + Mathf.Atan2(wallNormal.x, wallNormal.z) * Mathf.Rad2Deg, 0); 
        float lerpTime = onWall ? 0.75f : Mathf.Clamp(Mathf.InverseLerp(0, 2, Mathf.Abs(collisionInfo.velocity.x)) + Mathf.InverseLerp(0, 2, Mathf.Abs(collisionInfo.velocity.z)), 0, 1);
        visual.rotation = Quaternion.Slerp(savedRotation, targetRotation, Time.deltaTime * visualTurnSpeed * lerpTime);
    }

    IEnumerator JumpReset() {
        justJumped = true;
        yield return new WaitForSeconds(0.2f);
        justJumped = false;
    }
    IEnumerator Landing() {
        jumpsRemaining = collisionInfo.grounded ? (collisionInfo.sliding || collisionInfo.slidingLastFrame) ? baseJumps : baseJumps + extraJumps : jumpsRemaining;
        lastWallUsed = null;
        //Debug.Log("Landing: " + priorJumpVelocity + " " + Mathf.InverseLerp(10, 20, priorJumpVelocity));
        float startingTime = GameManager.GameTime;
        float priorJumpVelocity = Mathf.Abs(collisionInfo.velocityPriorFrame.y);
        while (collisionInfo.grounded && GameManager.GameTime - startingTime < LANDING_ANIM_TIME) {
            visual.localScale = new Vector3(visual.localScale.x, Mathf.Lerp(1, landingYScale.Evaluate(Mathf.InverseLerp(0, LANDING_ANIM_TIME, GameManager.GameTime - startingTime)), Mathf.InverseLerp(10, 20, priorJumpVelocity)), visual.localScale.z);
            yield return new WaitForEndOfFrame();
        }
        visual.localScale = Vector3.one;
    }

    public void PlatformAttatchmentToggle(bool state, Transform platformOrNull) {
        transform.SetParent(platformOrNull);
        onPlatform = state;
        platformTransform = platformOrNull;
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
        playerController.CancelMomentum();
    }

    private void EnterWalljump(GameObject wall, Vector3 normal) {
        if (wall == lastWallUsed) {
            Debug.Log("ayy");
            //return;
        }
        Debug.Log(normal);
        CancelMomentum();
        onWall = true;
        holdingWallStartTime = GameManager.GameTime;
        wallNormal = normal;
        lastWallUsed = wall;
    }

    private IEnumerator ExitWalljump(bool jumpOut) {
        onWall = false;
        holdingWallStartTime = 0;
        wallNormal = Vector3.zero;
        if(jumpOut) {
            float startTime = GameManager.GameTime;
            while(GameManager.GameTime - startTime < CONTROL_POST_WALLJUMP) {
                globalMovementModifier = Mathf.Lerp(0.2f, 0.8f, Mathf.InverseLerp(0, CONTROL_POST_WALLJUMP, GameManager.GameTime - startTime));
                yield return new WaitForEndOfFrame();
            }
            globalMovementModifier = 1;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        //Debug.Log(collision.transform.name);
        if (!collisionInfo.grounded && collision.transform.GetComponent<Surface>() && collision.transform.GetComponent<Surface>().walljump && collision.GetContact(0).normal.y < 0.05f && collision.GetContact(0).normal.y > -0.05f) {
            EnterWalljump(collision.gameObject, collision.GetContact(0).normal);
        }
    }

    private void OnCollisionExit(Collision collision) {
        if (onWall && !collisionInfo.grounded && collision.transform.GetComponent<Surface>() && collision.transform.GetComponent<Surface>().walljump) {
            //StartCoroutine(ExitWallJumpDelay());
        }
    }
}
