using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public interface IDamageable { void Hit(Vector2 from, int damage); }

public class PlayerController : MonoBehaviour
{
    public BoardGrid board;
    public LayerMask enemyMask;

    [Header("Step")]
    public float stepTime = 0.10f;
    public float stepCooldown = 0.03f;

    [Header("Dash")]
    public float dashChargeTime = 1.5f;
    public bool canMoveWhileCharging = false;
    public Color chargeColor = new Color(1f, 0.85f, 0.4f);
    public Color readyColor = Color.white;

    public float ChargeProgress => Mathf.Clamp01(chargeHeld / dashChargeTime);
    private bool isCharging;
    private float chargeHeld;

    public int dashDistance = 2;
    public float dashTime = 0.12f;
    public float dashCooldown = 0.5f;
    [Tooltip("Dash pressed slightly early still counts")]
    public float dashBuffer = 0.15f;
    public int dashDamage = 1;

    [Header("Health")]
    public int maxHealth = 3;
    public float invincibilityTime = 1.2f;
    public float blinkInterval = 0.08f;
    [Tooltip("Contact radius as a fraction of cell size")]
    public float contactRadius = 0.35f;
    public UnityEvent<int, int> onHealthChanged;   // current, max
    public UnityEvent onHit;                       // sound / screen shake
    public UnityEvent onDeath;                     // game over

    public int Health { get; private set; }
    public bool IsDead => Health <= 0;
    public bool IsInvincible => invincibleLeft > 0f;
    private float invincibleLeft;

    [Header("Visuals")]
    public SpriteRenderer sprite;   // auto-found in children if left empty

    private InputAction moveAction, dashAction;
    private Vector2Int inputDir;
    private Vector2Int lastDir = Vector2Int.down;
    private float dashBufferLeft, dashCooldownLeft, stepCooldownLeft;
    private bool isMoving;

    public Animator root;
    public Animator playerAnimator;

    public Vector2Int Cell { get; private set; }
    public bool IsDashing { get; private set; }

    private void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();

        Health = maxHealth;   // <-- add this

    }

    private void OnEnable()
    {
        moveAction.Enable(); dashAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable(); dashAction.Disable();
    }

    private void OnDash(InputAction.CallbackContext _) => dashBufferLeft = dashBuffer;

    private void Start()
    {
        Cell = board.WorldToCell(transform.position);
        transform.position = board.CellToWorld(Cell, transform.position.z);
        onHealthChanged?.Invoke(Health, maxHealth);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (IsDead) return;
        invincibleLeft -= Time.deltaTime;
        CheckContact();

        dashCooldownLeft -= Time.deltaTime;
        stepCooldownLeft -= Time.deltaTime;

        // Read movement input (horizontal OR vertical, never diagonal)
        Vector2 raw = moveAction.ReadValue<Vector2>();
        if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
            inputDir = Mathf.Abs(raw.x) > 0.5f ? new Vector2Int((int)Mathf.Sign(raw.x), 0) : Vector2Int.zero;
        else
            inputDir = Mathf.Abs(raw.y) > 0.5f ? new Vector2Int(0, (int)Mathf.Sign(raw.y)) : Vector2Int.zero;

        if (inputDir != Vector2Int.zero)
            lastDir = inputDir;

        // Face left / right
        if (sprite && inputDir.x != 0)
            sprite.flipX = inputDir.x < 0;

        if (isMoving) return;

        // ---- Dash charge ----
        if (!isCharging && dashAction.WasPressedThisFrame() && dashCooldownLeft <= 0f)
        {
            isCharging = true;
            chargeHeld = 0f;

            playerAnimator.SetBool("Charging", true);
        }

        if (isCharging)
        {
            if (dashAction.IsPressed())
            {
                chargeHeld += Time.deltaTime;
                UpdateChargeVisual();
            }
            else
            {
                // Released: dash only if fully charged
                bool ready = chargeHeld >= dashChargeTime;
                CancelCharge();
                if (ready) TryDash();
                return;
            }

            if (!canMoveWhileCharging) return;
        }

        // ---- Normal movement ----
        if (inputDir != Vector2Int.zero && stepCooldownLeft <= 0f)
            TryStep(inputDir);
    }

    private void UpdateChargeVisual()
    {
        if (!sprite) return;
        // Tint while charging, then pulse once fully charged
        sprite.color = chargeHeld >= dashChargeTime
            ? Color.Lerp(chargeColor, readyColor, Mathf.PingPong(Time.time * 8f, 1f))
            : Color.Lerp(readyColor, chargeColor, ChargeProgress);
    }

    private void CancelCharge()
    {
        isCharging = false;
        chargeHeld = 0f;
        if (sprite) sprite.color = readyColor;

        playerAnimator.SetBool("Charging", false);
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
        CancelCharge();

        isMoving = true;
        IsDashing = dash; // Important: set this BEFORE animation checks

        Vector3 start = transform.position;
        Vector3 end = board.CellToWorld(target, start.z);

        Cell = target;

        if (!dash)
        {
            root.ResetTrigger("HopEnd");
            root.SetTrigger("Hop");
        }

        if (dash)
            Slash(start, end);

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float e = dash
                ? 1f - (1f - t) * (1f - t)
                : t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(start, end, e);
            yield return null;
        }

        transform.position = end;

        if (!dash)
            root.SetTrigger("HopEnd");

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
    // Enemies call this on their attacks; CheckContact calls it when you touch one
    public void TakeHit()
    {
        if (IsDead || IsDashing || IsInvincible) return;

        Health--;
        CancelCharge();
        onHit?.Invoke();
        onHealthChanged?.Invoke(Health, maxHealth);

        if (IsDead) { Die(); return; }
        StartCoroutine(Invincibility());
    }

    private void CheckContact()
    {
        if (IsDashing || IsInvincible) return;
        if (Physics2D.OverlapCircle(transform.position, board.cellSize * contactRadius, enemyMask))
            TakeHit();
    }

    private IEnumerator Invincibility()
    {
        invincibleLeft = invincibilityTime;
        while (invincibleLeft > 0f)
        {
            if (sprite) sprite.enabled = !sprite.enabled;   // blink
            yield return new WaitForSeconds(blinkInterval);
        }
        if (sprite) sprite.enabled = true;
    }

    private void Die()
    {
        if (sprite) sprite.enabled = true;
        Debug.Log("Player died!");
        onDeath?.Invoke();
    }
}