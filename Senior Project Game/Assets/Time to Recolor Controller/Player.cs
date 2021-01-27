using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class Player : Singleton<Player> {

    [Header("Basic Movement")]
    public float baseMoveSpeed = 4;
    float moveSpeed = 4;
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = 0.4f;
    float accelerationTimeAirborne = 0.2f;
    float accelerationTimeGrounded = 0.1f;

    [Header("Wall Jumping & Sliding")]
    bool wallSliding = false;
    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;
    public float wallSlideSpeedMax = 3;
    public float wallStickTime = 0.25f;
    float timeToWallUnstick;

    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing;

    Controller2D controller;
    PlayerInputManager inputManager;
    Material material;

    [Header("Skill Options")]
    public int maximumJumps = 2;
    int jumpsRemaining;
    public bool wallJumpingEnabled = true;
    public bool freeplay;
    public int freeplayColorStep = 0;

    private AudioSource swapSound;

    public ColorSwatch color = ColorSwatch.Blue;

    //public Stat health;

    Transform visual;
    Animator animator;
    bool facingRight = true;
    int directionX = 1;
    bool isWalking;
    bool isSprinting;
    bool isAirborne;
    bool jumpKeyDownQueued;
    bool jumpKeyUpQueued;
    [HideInInspector] public bool justJumped = false;

    bool isAttackState;
    float attackLength = 0.5f;

    void Start() {
        controller = GetComponent<Controller2D>();
        inputManager = GetComponent<PlayerInputManager>();
        visual = transform.GetChild(0);
        animator = visual.GetComponent<Animator>();
        material = visual.GetComponentInChildren<Renderer>().sharedMaterial;
        swapSound = GetComponent<AudioSource>();

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        
        print("Gravity: " + gravity + "  Jump Velocity: " + maxJumpVelocity);

        ChangeColor(color, true);
    }


    private void Update() {
        if (GameManager.Instance.gameState == GameState.Regular) {
            if (inputManager.JumpKeyDown)
                jumpKeyDownQueued = true;
            if (inputManager.JumpKeyUp)
                jumpKeyUpQueued = true;
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E)) && !wallSliding && !isAttackState) {
                StartCoroutine(Attack());
            }
            if(Input.GetKeyDown(inputManager.FreeplayColor) && freeplay) {
                freeplayColorStep++;
                if(freeplayColorStep > ColorManager.Instance.colors.Length - 1) {
                    freeplayColorStep = 0;
                }
                ChangeColor((ColorSwatch)freeplayColorStep, true);
                swapSound.Play();
            }
        }
    }

    void FixedUpdate() {
        if (GameManager.Instance.gameState == GameState.Regular) {
            isSprinting = inputManager.Sprint;
            //moveSpeed = baseMoveSpeed * ((isSprinting && controller.collisions.below) ? 2 : 1);
            moveSpeed = baseMoveSpeed * ((isSprinting) ? 1.6f : 1);
            Vector2 input = new Vector2(inputManager.HorizontalMotion, inputManager.VerticallMotion);
            int wallDirX = (controller.collisions.left) ? -1 : 1;

            float targetVelocityX = input.x * moveSpeed;
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);

            jumpsRemaining = controller.collisions.below ? maximumJumps : jumpsRemaining;

            wallSliding = false;
            if (wallJumpingEnabled) {
                //Removing && velocity.y < 0 allows for climbing
                if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0) {
                    wallSliding = true;
                    if (velocity.y < -wallSlideSpeedMax) {
                        velocity.y = -wallSlideSpeedMax;
                    }
                    if (timeToWallUnstick > 0) {
                        velocityXSmoothing = 0;
                        velocity.x = 0;
                        if (input.x != wallDirX && input.x != 0) {
                            timeToWallUnstick -= Time.deltaTime;
                        }
                        else {
                            timeToWallUnstick = wallStickTime;
                        }
                    }
                    else {
                        timeToWallUnstick = wallStickTime;
                    }

                }
            }
            if (jumpKeyDownQueued) {
                jumpKeyDownQueued = false;
                if (wallSliding) {
                    if (wallDirX == input.x) {
                        velocity.x = -wallDirX * wallJumpClimb.x;
                        velocity.y = wallJumpClimb.y;
                    }
                    else if (input.x == 0) {
                        velocity.x = -wallDirX * wallJumpOff.x;
                        velocity.y = wallJumpOff.y;
                    }
                    else {
                        velocity.x = -wallDirX * wallLeap.x;
                        velocity.y = wallLeap.y;
                    }
                }
                if (controller.collisions.below || jumpsRemaining > 0) {
                    velocity.y = maxJumpVelocity;
                    StartCoroutine(JustJumped());
                    jumpsRemaining--;
                }
            }
            if (jumpKeyUpQueued) {
                jumpKeyUpQueued = false;
                if (velocity.y > minJumpVelocity) {
                    velocity.y = minJumpVelocity;
                }
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime, input);

            if (controller.collisions.above || controller.collisions.below) {
                velocity.y = 0;
            }

            isAirborne = !controller.collisions.below;
            isWalking = (input.x != 0) && !isAirborne;
            AnimateCharacter(velocity, (wallDirX == -1 && wallSliding) ? true : false);
        } else if (GameManager.Instance.gameState == GameState.Rewind) {
            AnimateCharacterRewind(GameManager.Instance.RequestFrameState());
        }
    }

    IEnumerator JustJumped() {
        justJumped = true;
        yield return new WaitForSeconds(0.1f);
        justJumped = false;
    }

    void AnimateCharacter(Vector3 direction, bool wallSlideOverrideFix) {
        if (direction.x > 0) {
            visual.transform.rotation = Quaternion.Euler(visual.transform.rotation.x, 0, visual.transform.rotation.z);
            facingRight = true;
        }
        else if (direction.x < 0) {
            visual.transform.rotation = Quaternion.Euler(visual.transform.rotation.x, 180, visual.transform.rotation.z);
            facingRight = false;
        }
        //visual.transform.rotation = Quaternion.Euler(visual.transform.rotation.x, visual.transform.rotation.y, controller.collisions.slopeAngle);
        directionX = facingRight ? 1 : -1;
        animator.SetBool("isWalking", isWalking && !wallSliding && !isSprinting);
        animator.SetBool("isSprinting", isSprinting && isWalking && !wallSliding);
        animator.SetBool("isAirborne", isAirborne);
        animator.SetBool("isWallSliding", wallSliding);
        if (wallSliding)
            animator.SetBool("isAttack", false);

        GameManager.Instance.SendPlayerData(isWalking, isSprinting, isAirborne, wallSliding, transform.position, transform.rotation, wallSlideOverrideFix ? false : facingRight, color);
    }

    IEnumerator Attack() {
        animator.SetBool("isAttack", true);
        isAttackState = true;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(new Vector2(transform.position.x, transform.position.y + 1f), new Vector2(0.5f, 3), 0f, facingRight ? Vector2.right : Vector2.left, 1.5f);
        for(int i = 0; i < hits.Length; i++) {
            AttackCollisionTriggered(hits[i]);
        }
        yield return new WaitForSeconds(attackLength);
        animator.SetBool("isAttack", false);

        yield return new WaitForSeconds(0.15f);
        isAttackState = false;
    }
    public void AttackCollisionTriggered(RaycastHit2D hit) {
        if (isAttackState && hit.transform.GetComponent<IInteractable>() != null) {
            hit.transform.GetComponent<IInteractable>().Interact();
            Debug.Log(hit.transform.name);
        }
    }

    void AnimateCharacterRewind(PlayerFrameState playerFrameState) {
        if(playerFrameState == null)
            return;
        transform.position = playerFrameState.position;
        //transform.rotation = playerFrameState.rotation;
        if (playerFrameState.facingRight) {
            visual.transform.rotation = Quaternion.Euler(visual.transform.rotation.x, 0, visual.transform.rotation.z);
        }
        else if (!playerFrameState.facingRight) {
            visual.transform.rotation = Quaternion.Euler(visual.transform.rotation.x, 180, visual.transform.rotation.z);
        }
        animator.SetBool("isWalking", playerFrameState.isWalking && !playerFrameState.isWallSliding && !playerFrameState.isSprinting);
        animator.SetBool("isSprinting", playerFrameState.isSprinting && playerFrameState.isWalking && !playerFrameState.isWallSliding);
        animator.SetBool("isAirborne", playerFrameState.isAirborne);
        animator.SetBool("isWallSliding", playerFrameState.isWallSliding);
        ChangeColor(playerFrameState.color);
    }

    public void ChangeColor(ColorSwatch colorInput, bool alwaysChange = false) {
        if (alwaysChange || color != colorInput) {
            material.SetColor("_Color", ColorManager.Instance.FetchColor(colorInput));
            color = colorInput;
            ColorManager.Instance.UpdateAllColorObjects(color);
        }
    }
}