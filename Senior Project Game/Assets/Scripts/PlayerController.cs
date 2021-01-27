using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour {

    [Header("Movement Settings")]
    public float speed = 5;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float sprintSpeed;
    private Vector2 input;
    public float height = 0.5f;
    Vector3 forward;
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    private Vector3 gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    private bool justJumped = false;
    public int maxJumps = 1;
    private int jumpsRemaining;
    Vector3 movementVelocity;
    Vector3 verticalVelocity;

    [Header("Collision")]
    public LayerMask ground;
    public bool grounded;
    private bool groundedLastFrame;
    public float heightPadding = 0.05f;
    new Rigidbody rigidbody;
    Bounds bounds;
    RaycastHit hitInfo;

    [Header("Slopes")]
    [Range(0, 90)] public float maxGroundAngle = 50;
    float groundAngle;
    public bool isSliding;
    float currentSlideAcceleration;
    Vector3 slideVelocity;

    [Header("Visuals")]
    public Transform visual;
    CameraController playerCamera;
    float angle;
    public float visualTurnSpeed = 10;
    Quaternion targetRotation;
    public bool debug;
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI jumpCounterText;
    private const string debugTextFormat = "Velocity: {3}\nGrounded: {0}\nSliding: {1} & {2} @ {5}\nVertical Velocity: {4}";

    private Animator animator;

    private void Start() {
        playerCamera = Camera.main.GetComponent<CameraController>();
        rigidbody = GetComponent<Rigidbody>();
        bounds = GetComponent<CapsuleCollider>().bounds;

        gravity = new Vector3(0, -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2), 0);
        maxJumpVelocity = Mathf.Abs(gravity.y) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity.y) * minJumpHeight);

        animator = transform.GetChild(0).GetChild(0).GetComponent<Animator>();

        rigidbody.centerOfMass = new Vector3(0, 0, 0);
        rigidbody.inertiaTensor = new Vector3(1, 1, 1);
    }

    Vector3 movementTotal;

    private void Update() {
        GetInput();
        CalculateDirection();
        CalculateForward();
        CheckGround();
        CalculateGroundAngle();
        ApplyGravity();

        animator.SetBool("Grounded", grounded);
        bool running = !Input.GetKey(KeyCode.LeftShift);
        bool sprinting = running && Input.GetKey(KeyCode.LeftControl);
        speed = running ? sprinting ? sprintSpeed : runSpeed : walkSpeed;
        animator.SetBool("Walking", (input.x != 0 || input.y != 0) & !running);
        animator.SetBool("Running", (input.x != 0 || input.y != 0) & running & !sprinting);
        animator.SetBool("Sprinting", (input.x != 0 || input.y != 0) & sprinting);
        animator.SetBool("Sliding", isSliding);

        jumpsRemaining = grounded ? isSliding ? 1 : maxJumps : jumpsRemaining;
        jumpCounterText.text = string.Format("Jumps: {0}", jumpsRemaining);


        if (debug) DrawDebugLines();

        movementVelocity = Vector3.zero;
        if (Mathf.Abs(input.x) + Mathf.Abs(input.y) > 0) {
            Rotate();
            Move();
        }

        movementTotal = (movementVelocity + slideVelocity);


        if (Input.GetKeyDown(KeyCode.Space)) {
            if (grounded || jumpsRemaining > 0) {
                jumpsRemaining--;
                StartCoroutine(JumpReset());
                verticalVelocity.y = maxJumpVelocity;
                animator.SetTrigger("JumpUp");
                if (isSliding) {
                    verticalVelocity.x += slideVelocity.normalized.x * currentSlideAcceleration * 0.9f;
                    verticalVelocity.z += slideVelocity.normalized.z * currentSlideAcceleration * 0.9f;
                } else {
                    verticalVelocity += movementVelocity * 0.5f;
                }
            }
        }
        if (Input.GetKeyUp(KeyCode.Space)) {
            if (verticalVelocity.y > minJumpVelocity) {
                verticalVelocity.y = minJumpVelocity;
            }
        }
        

       
       rigidbody.velocity = movementTotal + verticalVelocity;

    }

    private void FixedUpdate() {
        //Acceleration decay linked to physics
        if (isSliding) {
            currentSlideAcceleration *= 1.15f;
            currentSlideAcceleration = Mathf.Clamp(currentSlideAcceleration, 0, 15);
        }
        else if(grounded) {
            currentSlideAcceleration = 0.5f;
        }

        verticalVelocity.x *= 0.95f;
        verticalVelocity.z *= 0.95f;
    }

    private void GetInput() {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
    }

    private void CalculateDirection() {
        angle = Mathf.Atan2(input.x, input.y);
        angle *= Mathf.Rad2Deg;
        angle += playerCamera.sharpRotationAngle;
    }

    private void Rotate() {
        Quaternion savedRotation = visual.rotation;
        targetRotation = Quaternion.Euler(0, angle, 0);
        transform.rotation = targetRotation;
        visual.rotation = Quaternion.Slerp(savedRotation, targetRotation, Time.deltaTime * visualTurnSpeed);
    }

    private void Move() {
        float speedModifier = 1;
        if (groundAngle >= maxGroundAngle) speedModifier = 0.25f;
        if (!grounded) speedModifier = 0.75f;
        movementVelocity = forward * speed * speedModifier;
    }

    void CalculateForward() {
        if(!grounded) {
            forward = transform.forward;
            return;
        }

        forward = Vector3.Cross(transform.right, hitInfo.normal);
    }

    void CalculateGroundAngle() {
        if(!grounded) {
            groundAngle = 0;
            return;
        }
        groundAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
    }

    IEnumerator JumpReset() {
        justJumped = true;
        yield return new WaitForSeconds(0.2f);
        justJumped = false;
    }

    void CheckGround() {
        groundedLastFrame = grounded;
        if(Physics.BoxCast(transform.position, new Vector3(bounds.extents.x, 0.0125f, bounds.extents.z), -Vector3.up, out hitInfo, transform.rotation, height + heightPadding, ground) && !justJumped) {
            if(Vector3.Distance(transform.position, hitInfo.point) < height) {
                //Keep position leveled to the ground to prevent clipping
                transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * height, 5 * Time.deltaTime);
            }
            grounded = true;
        } else {
            grounded = false;
        }
        if (!groundedLastFrame && grounded) {
            //Landing frame
            animator.SetTrigger("Landing");
        }
    }

    void ApplyGravity() {
        if(!grounded) {
            verticalVelocity += gravity * Time.deltaTime;
        } else {
            verticalVelocity = Vector3.zero;
        }

        isSliding = groundAngle > maxGroundAngle;
        if (isSliding) {
            slideVelocity = Vector3.Cross(Vector3.Cross(Vector3.up, hitInfo.normal), hitInfo.normal) * currentSlideAcceleration;
        } else {
            slideVelocity = Vector3.zero;
        }

    }

    void DrawDebugLines() {
        Debug.DrawLine(transform.position, transform.position + forward * height * 2, Color.blue);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * height, Color.green);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * height * 2f, Color.red);
    }

    private void LateUpdate() {
        debugText.text = string.Format(debugTextFormat, grounded, isSliding, slideVelocity, movementTotal, verticalVelocity, System.Math.Round(currentSlideAcceleration, 2));
    }
}
