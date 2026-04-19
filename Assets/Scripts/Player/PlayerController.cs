using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody rb;
    public float moveSpeed;
    public float moveAcceleration;
    public float maxVelocity;
    private InputAction jumpAction;
    private InputAction moveAction;

    [Header("Jumping")]
    public GroundDetector groundDetector;
    public float jumpForce;
    public float airAcceleration;

    [Header("Audio")]
    public PlayerMovementSounds movementSounds;
    [Tooltip("Suppress the landing sound for this many seconds after the player first touches the ground (prevents a spawn-landing thud).")]
    public float landingSoundSuppressionAfterFirstGround = 1f;

    [Header("Animation")]
    public Animator animator;

    [Header("Debug")]
    public Vector3 inputDir;
    public float speedDebug;
    public bool groundDebug;

    private float footstepTimer;
    private bool wasGrounded;
    private float airTime;
    private float firstGroundedTime = -1f;

    private bool inCutscene = false;

    private void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");
        moveAction = InputSystem.actions.FindAction("Move");
        footstepTimer = FootstepInterval;
        wasGrounded = groundDetector != null && groundDetector.IsGrounded;
    }

    private void Update()
    {
        // If the game or cutscene is locking the player, suppress input
        if (GameManager.Instance.LOCKED || inCutscene)
        {
            inputDir = Vector3.zero;
            return;
        }

        HandleJumping();
        HandleMovement();
    }

    private void HandleJumping()
    {
        if (jumpAction.triggered && groundDetector.IsGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (movementSounds != null)
            {
                Vector3 pos = transform.position;
                movementSounds.jumpAir.PlayAt(pos);
                movementSounds.jumpStep.PlayAt(pos);
            }
            //animator.SetBool("isJumping", true);
        }

        if (groundDetector.IsGrounded)
        {
            //animator.SetBool("isJumping", false);
        }
    }

    private void HandleMovement()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        inputDir = new Vector3(moveValue.x, 0, moveValue.y);
    }

    private void FixedUpdate()
    {
        if (inCutscene) return;

        if (GameManager.Instance.LOCKED)
        {
            if (GameManager.Instance.MinigameActive)
            {
                Vector3 v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(0f, v.y, 0f);

                Vector3 gNormal = groundDetector != null ? groundDetector.GroundNormal : Vector3.up;
                rb.AddForce(-gNormal * Physics.gravity.magnitude * rb.mass, ForceMode.Acceleration);
            }
            return;
        }

        Vector3 moveDir = transform.TransformDirection(inputDir.normalized);
        float velocityX = Mathf.Clamp(moveDir.x * moveSpeed, -maxVelocity, maxVelocity);
        float velocityZ = Mathf.Clamp(moveDir.z * moveSpeed, -maxVelocity, maxVelocity);
        Vector3 targetVelocity = new Vector3(velocityX, rb.linearVelocity.y, velocityZ);

        bool wantsMove = inputDir.sqrMagnitude > 0.01f;
        bool grounded = groundDetector != null && groundDetector.IsGrounded;

        if (!grounded)
            airTime += Time.fixedDeltaTime;

        if (grounded && firstGroundedTime < 0f)
            firstGroundedTime = Time.time;

        if (grounded && !wasGrounded)
        {
            bool withinSpawnSuppressionWindow = firstGroundedTime < 0f
                || Time.time - firstGroundedTime < landingSoundSuppressionAfterFirstGround;
            if (!withinSpawnSuppressionWindow
                && movementSounds != null
                && airTime >= movementSounds.minAirTimeForLanding)
                movementSounds.landing.PlayAt(transform.position);
            airTime = 0f;
            footstepTimer = FootstepInterval;
        }
        wasGrounded = grounded;

        if (wantsMove && grounded)
        {
            footstepTimer -= Time.fixedDeltaTime;
            if (footstepTimer <= 0f)
            {
                if (movementSounds != null)
                    movementSounds.footsteps.PlayAt(transform.position);
                footstepTimer = FootstepInterval;
            }
        }
        else
        {
            footstepTimer = FootstepInterval;
        }

        float currentAcceleration = grounded ? moveAcceleration : airAcceleration;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, currentAcceleration * Time.deltaTime);

        Vector3 groundNormal = groundDetector != null ? groundDetector.GroundNormal : Vector3.up;
        Vector3 gravity = -groundNormal * Physics.gravity.magnitude * rb.mass;
        rb.AddForce(gravity, ForceMode.Acceleration);

        speedDebug = rb.linearVelocity.magnitude;
        groundDebug = grounded;
    }

    private float FootstepInterval => movementSounds != null ? movementSounds.footstepInterval : 0.45f;
}
