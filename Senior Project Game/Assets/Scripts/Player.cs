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
    const float WALLJUMP_INPUT_DELAY = 1;
    private GameObject lastWallUsed;
    const float CONTROL_POST_WALLJUMP = 0.5f;
    private bool controlTesting;
    const float MAX_WALL_VELOCITY = 30;
    private Coroutine walljumpTimerCoroutine;

    [Header("Surface")]
    public bool onPlatform;
    private Transform platformTransform;

    [Header("Visuals")]
    public Transform visual;
    private Animator animator;
    [HideInInspector] public CameraController playerCamera;
    float angle;
    public float visualTurnSpeed = 10;
    Quaternion targetRotation;
    public float visualMoveSpeed = 15;
    private Vector2 velocity2D;
    [SerializeField] private Transform blobShadow;
    //[SerializeField] private float blobShadowMaxDistance = 10;
    [SerializeField] private AnimationCurve blobShadowOpacity;
    private Material blobShadowMaterial;
    const float BLOB_MAX_OPACITY = 0.625f;
    [SerializeField] private ParticleSystem footstepParticleSystem;
    [SerializeField] private ParticleSystem footstepPoofParticleSystem;
    [SerializeField] private TrailRenderer[] spinRenderers;

    [Header("Interaction")]
    public bool justTeleported;
    private bool spinning;
    private bool spinQueued;
    private bool hasSpin = true;

    [Header("Debug")]
    public bool debug;
    public TextMeshProUGUI jumpCounterText;

    private void Start() {
        inputManager = GetComponent<PlayerInputManager>();
        playerCamera = Camera.main.transform.root.GetComponent<CameraController>();

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

        //blobShadowMaterial = blobShadow.GetComponent<MeshRenderer>().sharedMaterial;
    }

    private void Update() {
        input = new Vector2(inputManager.HorizontalMotion, inputManager.VerticallMotion);

        if (onWall) {
            if (inputManager.LateralInputExists) {
                input = Vector2.zero;
                if (!controlTesting) walljumpTimerCoroutine = StartCoroutine(WalljumpInputTimer());
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

        jumpCounterText.text = string.Format("Jumps: {0} Queued: {1}", jumpsRemaining, jumpQueued);

        AnimatePlayer(inputManager.LateralInputExists, savedRotation);

        if(Input.GetMouseButtonDown(0) && !spinning && !onWall && hasSpin) {
            spinQueued = true;
            StartCoroutine(ActivateSpin());
        }
    }

    private void FixedUpdate() {
        running = !inputManager.Walk;
        sprinting = running && inputManager.Sprint;
        speed = running ? sprinting ? sprintSpeed : runSpeed : walkSpeed;

        bool validJump = false;
        if (jumpQueued && (collisionInfo.grounded || jumpsRemaining > 0 || onWall) && !justJumped) {
            jumpQueued = false;
            StartCoroutine(JumpReset());
            validJump = true;
            jumpsRemaining--;
        }

        //Leaving Ground Frame
        if (collisionInfo.groundedLastFrame && !collisionInfo.grounded) {
            jumpsRemaining = extraJumps;
            footstepParticleSystem.Stop();
        }

        //Landing Frame
        if (!collisionInfo.groundedLastFrame && collisionInfo.grounded) {
            StartCoroutine(Landing());
            footstepParticleSystem.Play();
            footstepPoofParticleSystem.Emit(20);
        }

        if(!hasSpin) hasSpin = collisionInfo.grounded && !(collisionInfo.sliding || collisionInfo.slidingLastFrame);

        collisionInfo = playerController.CalculateFrameVelocity(input, speed, validJump, inputManager.JumpKeyUp, justJumped, onWall, wallNormal, globalMovementModifier, spinQueued);
        rigidbody.velocity = collisionInfo.velocity;
        velocity2D.x = rigidbody.velocity.x;
        velocity2D.y = rigidbody.velocity.z;

        if (onPlatform && !justJumped && onWall) {
            transform.localPosition = new Vector3(transform.localPosition.x, ((platformTransform.GetComponent<BoxCollider>().size.y * platformTransform.localScale.y) * 0.5f + 0.5f) / platformTransform.localScale.y, transform.localPosition.z);
        }

        if(onWall && validJump) {
            jumpsRemaining++;
            StartCoroutine(ExitWalljump(true));
        }

        spinQueued = false;

        if(spinning) {
            Collider[] spinHits = Physics.OverlapSphere(visual.position + new Vector3(0, 0.5f, 0), 0.65f);
            for(int i = 0; i < spinHits.Length; i++) {
                IHittable hittable;
                if(spinHits[i].TryGetComponent(out hittable)) {
                    Debug.Log("Find a hittable");
                    if (hittable.QueryHitDelay()) hittable.Hit();
                }
            }

        }

        if (debug) DrawDebugLines();
    }

    private void LateUpdate() {
        Vector3 targetPosition = transform.position;
        visual.transform.position = Vector3.Lerp(visual.transform.position, targetPosition, visualMoveSpeed * Time.deltaTime);
        /*RaycastHit blobShadowHit;
        if (Physics.Raycast(visual.position + new Vector3(0, 0.5f, 0), -visual.transform.up, out blobShadowHit, Mathf.Infinity, playerController.ground)) {
            blobShadow.position = new Vector3(visual.position.x, blobShadowHit.point.y, visual.position.z);
            blobShadowMaterial.SetColor("_BaseColor", new Color(0, 0, 0, BLOB_MAX_OPACITY * blobShadowOpacity.Evaluate(Mathf.InverseLerp(0, blobShadowMaxDistance, blobShadowHit.distance - 1))));
        }*/
    }

    private void CalculateDirection() {
        angle = Mathf.Atan2(input.x, input.y);
        angle *= Mathf.Rad2Deg;
        angle += playerCamera.trueRotationAngle;
        targetRotation = Quaternion.Euler(0, angle, 0);
        transform.rotation = targetRotation;
    }

    private void AnimatePlayer(bool horizontalInputExists, Quaternion savedRotation) {
        animator.SetBool("Grounded", collisionInfo.grounded);
        //animator.SetBool("Walking", horizontalInputExists & !running);
        //animator.SetBool("Running", horizontalInputExists & running & !sprinting);
        //animator.SetBool("Sprinting", horizontalInputExists & sprinting);
        animator.SetFloat("Horizontal Velocity", Mathf.InverseLerp(0, sprintSpeed, velocity2D.magnitude));
        animator.SetFloat("Smooth Horizontal Velocity", Mathf.InverseLerp(0, sprintSpeed, velocity2D.magnitude), 0.2f, Time.deltaTime);
        animator.SetBool("Sliding", collisionInfo.sliding);
        animator.SetFloat("Vertical Velocity", Mathf.InverseLerp(-10, 10, collisionInfo.velocity.y));
        animator.SetBool("Holding Wall", onWall);

        if(horizontalInputExists && collisionInfo.grounded) {
            if (!footstepParticleSystem.isPlaying) footstepParticleSystem.Play();
        } else {
            footstepParticleSystem.Stop();
        }

        float velocityAngle = Mathf.Atan2(collisionInfo.velocity.x, collisionInfo.velocity.z) * Mathf.Rad2Deg;

        targetRotation = Quaternion.Euler(0, (!onWall ? (horizontalInputExists ? angle : (velocity2D.magnitude > 0.5f ? collisionInfo.grounded ? velocityAngle : transform.eulerAngles.y : transform.eulerAngles.y)) : 180 + Mathf.Atan2(wallNormal.x, wallNormal.z) * Mathf.Rad2Deg), 0); 
        
        float lerpTime = onWall ? 0.75f : velocity2D.magnitude > 0.1f ? Mathf.Clamp01(Mathf.InverseLerp(0, 2, Mathf.Abs(collisionInfo.velocity.x)) + Mathf.InverseLerp(0, 2, Mathf.Abs(collisionInfo.velocity.z))) : 1;
        visual.rotation = Quaternion.Slerp(savedRotation, targetRotation, Time.deltaTime * visualTurnSpeed * lerpTime);
    }

    IEnumerator ActivateSpin() {
        spinning = true;
        hasSpin = false;
        animator.SetTrigger("Spin");
        animator.SetBool("Disable Transition", true);
        yield return new WaitForSeconds(0.1f);
        for(int i = 0; i < spinRenderers.Length; i++) {
            spinRenderers[i].emitting = true;
        }
        yield return new WaitForSeconds(0.4f);
        for (int i = 0; i < spinRenderers.Length; i++) {
            spinRenderers[i].emitting = false;
        }
        spinning = false;
        animator.SetBool("Disable Transition", false);
    }

    IEnumerator JumpReset() {
        justJumped = true;
        yield return new WaitForSeconds(0.2f);
        justJumped = false;
    }
    IEnumerator Landing() {
        jumpsRemaining = collisionInfo.grounded ? (collisionInfo.sliding || collisionInfo.slidingLastFrame) ? baseJumps : baseJumps + extraJumps : jumpsRemaining;
        lastWallUsed = null;
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

    public IEnumerator JustTeleported() {
        justTeleported = true;
        yield return new WaitForSeconds(Teleporter.TELEPORT_COOLDOWN);
        justTeleported = false;
    }

    public void CancelMomentum() {
        playerController.CancelMomentum();
    }

    private void EnterWalljump(GameObject wall, Vector3 normal) {
        CancelMomentum();
        onWall = true;
        wallNormal = normal;
        transform.SetParent(wall.transform);
        lastWallUsed = wall;
        if(walljumpTimerCoroutine != null) StopCoroutine(walljumpTimerCoroutine);
        globalMovementModifier = 1;
    }

    private IEnumerator WalljumpInputTimer() {
        controlTesting = true;
        float startTime = GameManager.GameTime;
        bool stillDesiringMotion = true;
        while (GameManager.GameTime - startTime < WALLJUMP_INPUT_DELAY) {
            stillDesiringMotion = inputManager.LateralInputExists;
            if (!stillDesiringMotion) break;
            yield return new WaitForEndOfFrame();
        }
        controlTesting = false;
        if(stillDesiringMotion) StartCoroutine(ExitWalljump(false));
    }

    private IEnumerator ExitWalljump(bool jumpOut) {
        onWall = false;
        wallNormal = Vector3.zero;
        transform.SetParent(null);
        if (jumpOut) {
            globalMovementModifier = 0.2f;
            transform.Rotate(0, 180, 0);
            float startTime = GameManager.GameTime;
            while(GameManager.GameTime - startTime < CONTROL_POST_WALLJUMP) {
                globalMovementModifier = Mathf.Lerp(0.2f, 0.8f, Mathf.InverseLerp(0, CONTROL_POST_WALLJUMP, GameManager.GameTime - startTime));
                yield return new WaitForEndOfFrame();
            }
            globalMovementModifier = 1;
        }

    }

    private void OnCollisionEnter(Collision collision) {
        if (!collisionInfo.grounded && collision.transform.GetComponent<Surface>() && collision.transform.GetComponent<Surface>().walljump && collision.GetContact(0).normal.y < 0.05f && collision.GetContact(0).normal.y > -0.05f) {
            if(Mathf.Abs(collisionInfo.velocity.y) < MAX_WALL_VELOCITY) EnterWalljump(collision.gameObject, collision.GetContact(0).normal);
        }
    }

    private void OnCollisionStay(Collision collision) {
        if(onWall && collision.transform.GetComponent<Surface>() && collision.transform.GetComponent<Surface>().walljump && collision.GetContact(0).normal.y < 0.05f && collision.GetContact(0).normal.y > -0.05f) {
            wallNormal = collision.GetContact(0).normal;
        } else if (onWall) {
            StartCoroutine(ExitWalljump(false));
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Collectible")) {
            Collectible collectible = other.gameObject.GetComponent<Collectible>();
            if(!collectible.collected) collectible.Pickup(transform);
        }
    }

    void DrawDebugLines() {
        Debug.DrawLine(transform.position, transform.position + collisionInfo.forward * 0.5f * 2, Color.blue);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * 0.5f, Color.green);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * 0.5f * 2f, Color.red);
    }
}
