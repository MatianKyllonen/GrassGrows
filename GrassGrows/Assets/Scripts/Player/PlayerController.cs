using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    [Tooltip("How fast the player speeds up (units/sec²)")]
    public float acceleration = 60f;
    [Tooltip("How fast the player slows down when no input is held (units/sec²)")]
    public float deceleration = 80f;

    [Header("Dash")]
    public float dashSpeed = 24f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.6f;

    private Rigidbody2D rb;
    private InputAction moveAction;
    private InputAction dashAction;

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;

    private bool dashRequested;
    private bool isDashing;
    private float dashTimeLeft;
    private float dashCooldownLeft;
    private Vector2 dashDirection;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Movement: WASD, arrow keys, gamepad left stick
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Dash: Space, gamepad south button (A / Cross)
        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
        dashAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        dashAction.performed -= OnDashPerformed;
        moveAction.Disable();
        dashAction.Disable();
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        dashRequested = true;
    }

    private void Update()
    {
        moveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        if (moveInput.sqrMagnitude > 0.01f)
            lastDirection = moveInput.normalized;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        dashCooldownLeft -= dt;

        // Start a dash if requested
        if (dashRequested)
        {
            dashRequested = false;
            if (!isDashing && dashCooldownLeft <= 0f)
                StartDash();
        }

        // Dashing overrides normal movement
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            dashTimeLeft -= dt;
            if (dashTimeLeft <= 0f)
                EndDash();
            return;
        }

        // Smooth movement
        Vector2 targetVelocity = moveInput * moveSpeed;
        float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, rate * dt);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;
        dashCooldownLeft = dashCooldown;
        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : lastDirection;
    }

    private void EndDash()
    {
        isDashing = false;
        // Drop back to run speed so the exit blends into normal movement
        rb.linearVelocity = dashDirection * moveSpeed;
    }
}