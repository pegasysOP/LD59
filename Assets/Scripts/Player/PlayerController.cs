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

    [Header("Animation")]
    public Animator animator;

    [Header("Debug")]
    public Vector3 inputDir;
    public float speedDebug;
    public bool groundDebug;

    private float movementTimer = 0.5f;
    private float movementDelay = 0.5f;

    private void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");
        moveAction = InputSystem.actions.FindAction("Move");
    }

    private void Update()
    {
        movementTimer -= Time.deltaTime;
        if (GameManager.Instance.LOCKED)
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
        Vector3 moveDir = transform.TransformDirection(inputDir.normalized);
        float velocityX = Mathf.Clamp(moveDir.x * moveSpeed, -maxVelocity, maxVelocity);
        float velocityZ = Mathf.Clamp(moveDir.z * moveSpeed, -maxVelocity, maxVelocity);
        Vector3 targetVelocity = new Vector3(velocityX, rb.linearVelocity.y, velocityZ);

        if (inputDir != Vector3.zero)
        {
            if (movementTimer <= 0)
            {
                //AudioManager.Instance.PlaySfxWithPitchShifting(AudioManager.Instance.walkingClips);
                movementTimer = movementDelay;
            }
            //animator.SetBool("isMoving", true);
        }
        else
        {
            //animator.SetBool("isMoving", false);
        }

        float currentAcceleration = groundDetector.IsGrounded ? moveAcceleration : airAcceleration;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, currentAcceleration * Time.deltaTime);

        Vector3 gravity = -groundDetector.GroundNormal * Physics.gravity.magnitude * rb.mass;
        rb.AddForce(gravity, ForceMode.Acceleration);

        speedDebug = rb.linearVelocity.magnitude;
        groundDebug = groundDetector.IsGrounded;
    }
}