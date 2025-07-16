using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlanetGravityController : MonoBehaviour
{
    [Header("References")]
    public Transform gravityCenter;
    public Transform groundCheckOrigin;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Header("Jumping")]
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.6f;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundMask;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;

    private Rigidbody rb;
    private Vector3 gravityUp;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isGrounded;
    private Camera cam;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        cam = Camera.main;

        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Default", "Ground");
    }

    void Update()
    {
        if (!gravityCenter) return;

        gravityUp = (transform.position - gravityCenter.position).normalized;

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            PerformJump();
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        jumpBufferTimer -= Time.deltaTime;
        coyoteTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!gravityCenter) return;

        ApplyGravity();
        CheckGround();
        HandleMovement();
    }

    void ApplyGravity()
    {
        rb.AddForce(-gravityUp * 20f, ForceMode.Acceleration);
        AlignToGravity();
    }

    void AlignToGravity()
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
    }

    void CheckGround()
    {
        Vector3 origin = groundCheckOrigin ? groundCheckOrigin.position : transform.position;
        Vector3 rayOrigin = origin - gravityUp * 0.1f;

        isGrounded = Physics.SphereCast(rayOrigin, groundCheckRadius, -gravityUp, out RaycastHit hit, groundCheckDistance, groundMask);

        Debug.DrawRay(rayOrigin, -gravityUp * groundCheckDistance, isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            float downwardSpeed = Vector3.Dot(rb.linearVelocity, -gravityUp);
            if (downwardSpeed < 0.5f)
                coyoteTimer = coyoteTime;
        }
    }

    void HandleMovement()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3 verticalVel = Vector3.Project(rb.linearVelocity, gravityUp);

        if (input.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = verticalVel;
            return;
        }

        Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, gravityUp).normalized;
        Vector3 camRight = Vector3.Cross(gravityUp, camForward);
        Vector3 moveDir = (camForward * input.z + camRight * input.x).normalized;

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        Vector3 horizontalVel = moveDir * speed;

        rb.linearVelocity = horizontalVel + verticalVel;
    }

    void PerformJump()
    {
        Vector3 vel = rb.linearVelocity;

        // Remove downward velocity for consistent jump
        Vector3 downwardVel = Vector3.Project(vel, -gravityUp);
        vel -= downwardVel;

        // Apply upward jump impulse
        vel += gravityUp * jumpForce;

        rb.linearVelocity = vel;
    }
}
