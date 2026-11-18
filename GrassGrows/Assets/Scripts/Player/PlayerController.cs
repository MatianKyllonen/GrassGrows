using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IDamageable { void Hit(Vector2 from, int damage); }

public class PlayerController : MonoBehaviour
{
    public BoardGrid board;
    public LayerMask enemyMask;

    [Header("Step")]
    public float stepTime = 0.10f;
    public float stepCooldown = 0.03f;

    [Header("Dash")]
    public int dashDistance = 2;
    public float dashTime = 0.12f;
    public float dashCooldown = 0.5f;
    [Tooltip("Dash pressed slightly early still counts")]
    public float dashBuffer = 0.15f;
    public int dashDamage = 1;

    private InputAction moveAction, dashAction;
    private Vector2Int inputDir;
    private Vector2Int lastDir = Vector2Int.down;
    private float dashBufferLeft, dashCooldownLeft, stepCooldownLeft;
    private bool isMoving;

    public Vector2Int Cell { get; private set; }
    public bool IsDashing { get; private set; }

    private void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.Enable(); dashAction.Enable();
        dashAction.performed += OnDash;
    }

    private void OnDisable()
    {
        dashAction.performed -= OnDash;
        moveAction.Disable(); dashAction.Disable();
    }

    private void OnDash(InputAction.CallbackContext _) => dashBufferLeft = dashBuffer;

    private void Start()
    {
        Cell = board.WorldToCell(transform.position);
        transform.position = board.CellToWorld(Cell, transform.position.z);
    }

private void Update()
{
    dashCooldownLeft -= Time.deltaTime;
    stepCooldownLeft -= Time.deltaTime;
    dashBufferLeft -= Time.deltaTime;

    // Read movement input.
    Vector2 raw = moveAction.ReadValue<Vector2>();

    // Allow horizontal OR vertical movement, never diagonal.
    if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
    {
        inputDir = Mathf.Abs(raw.x) > 0.5f
            ? new Vector2Int((int)Mathf.Sign(raw.x), 0)
            : Vector2Int.zero;
    }
    else
    {
        inputDir = Mathf.Abs(raw.y) > 0.5f
            ? new Vector2Int(0, (int)Mathf.Sign(raw.y))
            : Vector2Int.zero;
    }

    // Remember the last direction for dashing.
    if (inputDir != Vector2Int.zero)
        lastDir = inputDir;

    // Don't interrupt an ongoing movement.
    if (isMoving)
        return;

    // Dash takes priority over normal movement.
    if (dashBufferLeft > 0f && dashCooldownLeft <= 0f)
    {
        if (TryDash())
            return;
    }

    // Normal movement.
    if (inputDir != Vector2Int.zero && stepCooldownLeft <= 0f)
    {
        TryStep(inputDir);
    }
}

    private void TryStep(Vector2Int dir)
    {
        Vector2Int target = Cell + dir;
        if (board.IsBlocked(target)) return;
        StartCoroutine(MoveTo(target, stepTime, false));
    }

    private bool TryDash()
    {
        Vector2Int dir = inputDir != Vector2Int.zero ? inputDir : lastDir;

        // Go as far as possible up to dashDistance; cancel if even 1 square is blocked
        int n = 0;
        for (int i = 1; i <= dashDistance; i++)
        {
            if (board.IsBlocked(Cell + dir * i)) break;
            n = i;
        }
        if (n == 0) return false;

        dashBufferLeft = 0f;
        dashCooldownLeft = dashCooldown;
        StartCoroutine(MoveTo(Cell + dir * n, dashTime, true));
        return true;
    }

    private IEnumerator MoveTo(Vector2Int target, float duration, bool dash)
    {
        isMoving = true;
        IsDashing = dash;

        Vector3 start = transform.position;
        Vector3 end = board.CellToWorld(target, start.z);
        Cell = target;   // logical position updates immediately so enemies see it

        if (dash) Slash(start, end);

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float e = dash ? 1f - (1f - t) * (1f - t)   // ease-out: snappy burst
                           : t * t * (3f - 2f * t);     // smoothstep
            transform.position = Vector3.Lerp(start, end, e);
            yield return null;
        }

        transform.position = end;
        IsDashing = false;
        isMoving = false;
        stepCooldownLeft = stepCooldown;
    }

    // Moving through enemies cuts them down
    private void Slash(Vector3 a, Vector3 b)
    {
        float radius = board.cellSize * 0.4f;
        var hits = Physics2D.CircleCastAll(a, radius, (b - a).normalized,
                                           Vector3.Distance(a, b), enemyMask);
        foreach (var h in hits)
            if (h.collider.TryGetComponent<IDamageable>(out var d)) d.Hit(a, dashDamage);
    }

    // Enemies call this. Dash gives brief invulnerability.
    public void TakeHit()
    {
        if (IsDashing) return;
        Debug.Log("Player hit!");   // hook up health here
    }
}