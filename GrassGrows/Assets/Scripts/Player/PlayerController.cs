
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public interface IDamageable
{
    void Hit(Vector2 from, int damage);
}

public class PlayerController : MonoBehaviour
{
    public BoardGrid board;
    public LayerMask enemyMask;

    [Header("Step")]
    public float stepTime = 0.10f;
    public float stepCooldown = 0.03f;
    public float hopClipLength = 0.3f;   // length of your Hop clip in seconds

    [Header("Dash")]
    public float dashChargeTime = 1.5f;
    public bool canMoveWhileCharging = false;
    public int dashDistance = 2;
    public float dashTime = 0.12f;
    public float dashCooldown = 0.5f;
    public int dashDamage = 1;
    public event System.Action<Vector2Int, Vector2Int> onDashed;   // fromCell, toCell
    private int lastChargeTick;

    public float ChargeProgress =>
        Mathf.Clamp01(chargeHeld / dashChargeTime);

    private bool isCharging;
    private float chargeHeld;

    [Header("Dash Flash")]
    public float readyPulseSpeed = 5f;

    [Range(0f, 1f)]
    public float minFlashOpacity = 0.2f;

    [Range(0f, 1f)]
    public float maxFlashOpacity = 0.65f;

    private MaterialPropertyBlock flashProperties;

    public float attackBuffer = 0.2f;
    private float attackBufferLeft;
    public float dashBuffer = 0.2f;
    private float dashBufferLeft;

    private bool dashQueued;

    [Header("Attack")]
    public int attackDamage = 1;
    public float attackStartup = 0.12f;
    public float attackActive = 0.06f;
    public float attackRecovery = 0.25f;
    public GameObject attackEffectPrefab;

    [Header("Health")]
    public int maxHealth = 3;
    public float invincibilityTime = 1.2f;
    public float blinkInterval = 0.08f;

    [Header("Heal")]
    public float healHoldTime = 1f;
    public int healAmount = 1;
    [Range(0f, 1f)] public float healXpCostFraction = 0.25f;
    public UnityEvent onHealed;   // sound / green flash

    public float HealProgress => Mathf.Clamp01(healHeld / healHoldTime);   // for a UI ring

    private InputAction healAction;
    private bool isHealing;
    private bool healLatch;   // must release J before another heal can start
    private float healHeld;

    [Tooltip("Contact radius as a fraction of cell size")]
    public float contactRadius = 0.35f;

    public UnityEvent<int, int> onHealthChanged;
    public UnityEvent onHit;
    public UnityEvent onDeath;

    [Header("Visuals")]
    public SpriteRenderer sprite;
    public Animator root;
    public Animator playerAnimator;

    [Header("Audio")]
    public AudioClip dashTickSFX;
    public AudioClip dashSlashSFX;
    public AudioClip slashSFX;
    public AudioClip healSFX;

    // ---------- Public state ----------

    public Vector2Int Cell { get; private set; }
    public bool IsDashing { get; private set; }
    public int Health { get; private set; }
    public bool IsDead => Health <= 0;
    public bool IsInvincible => invincibleLeft > 0f;

    // ---------- Private state ----------

    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction attackAction;

    private Vector2Int inputDir;
    private Vector2Int lastDir = Vector2Int.down;
    private int facingX = 1;   // 1 = right, -1 = left. Attacks only go this way

    private float dashCooldownLeft;
    private float stepCooldownLeft;
    private float invincibleLeft;



    private bool isMoving;

    private bool isAttacking;
    private Coroutine attackRoutine;

    // ---------- Initialization ----------

    private void Awake()
    {
        Health = maxHealth;

        moveAction = new InputAction(
            "Move",
            InputActionType.Value
        );

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

        healAction = new InputAction("Heal", InputActionType.Button);
        healAction.AddBinding("<Keyboard>/j");
        healAction.AddBinding("<Gamepad>/buttonNorth");

        dashAction = new InputAction(
            "Dash",
            InputActionType.Button
        );

        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");

        attackAction = new InputAction(
            "Attack",
            InputActionType.Button
        );

        attackAction.AddBinding("<Keyboard>/k");
        attackAction.AddBinding("<Gamepad>/buttonWest");

        if (!sprite)
            sprite = GetComponentInChildren<SpriteRenderer>();

        flashProperties = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
        attackAction.Enable();
        healAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();
        attackAction.Disable();
        healAction.Disable();
    }

    private void Start()
    {

        Cell = board.WorldToCell(transform.position);

        transform.position = board.CellToWorld(
            Cell,
            transform.position.z
        );

        onHealthChanged?.Invoke(Health, maxHealth);
        RefreshHopSpeed();
        RefreshChargeSpeed();
    }

    public void RefreshHopSpeed()
    {
        if (root) root.SetFloat("HopSpeed", hopClipLength / stepTime);
    }

    public void RefreshChargeSpeed()
    {
        if (playerAnimator) playerAnimator.SetFloat("ChargeSpeed", 0.75f / dashChargeTime);
    }

    // ---------- Update ----------

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (Time.timeScale == 0f) return;
        if (IsDead) return;

        invincibleLeft -= Time.deltaTime;
        CheckContact();

        dashCooldownLeft -= Time.deltaTime;
        stepCooldownLeft -= Time.deltaTime;
        attackBufferLeft -= Time.deltaTime;

        // Read movement input (horizontal OR vertical, never diagonal)
        Vector2 raw = moveAction.ReadValue<Vector2>();
        if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
            inputDir = Mathf.Abs(raw.x) > 0.5f ? new Vector2Int((int)Mathf.Sign(raw.x), 0) : Vector2Int.zero;
        else
            inputDir = Mathf.Abs(raw.y) > 0.5f ? new Vector2Int(0, (int)Mathf.Sign(raw.y)) : Vector2Int.zero;

        if (!isAttacking)
        {
            if (inputDir != Vector2Int.zero)
                lastDir = inputDir;

            if (inputDir.x != 0)
            {
                facingX = inputDir.x;
                if (sprite) sprite.flipX = facingX < 0;
            }
        }

        bool attackPressed = attackAction.WasPressedThisFrame();
        bool inHop = isMoving && !IsDashing;   // buffering is only allowed during a normal hop

        // Attack: press or hold of K during a hop is remembered
        if (inHop && !isCharging && (attackPressed || attackAction.IsPressed()))
            attackBufferLeft = attackBuffer;

        // Dash: Space pressed during a hop queues a charge for when the hop ends
        // Dash: buffer presses even while idle or moving
        if (dashAction.WasPressedThisFrame())
        {
            dashBufferLeft = dashBuffer;
            dashQueued = true;
        }

        // Holding Space keeps the dash request alive
        if (dashAction.IsPressed() && dashQueued)
        {
            dashBufferLeft = dashBuffer;
        }

        // Expire the buffer if Space was released and the timer ran out
        if (dashBufferLeft > 0f)
        {
            dashBufferLeft -= Time.deltaTime;
        }
        else if (!dashAction.IsPressed())
        {
            dashQueued = false;
        }

        if (isMoving) return;
        if (isAttacking) return;

        // ---- Heal (hold J) ----
        if (!healAction.IsPressed()) healLatch = false;

        if (isHealing)
        {
            playerAnimator.SetBool("isHealing", true);
            if (!healAction.IsPressed() || !CanHeal())
            {
                CancelHeal();
            }
            else
            {
                healHeld += Time.deltaTime;
                if (healHeld >= healHoldTime) CompleteHeal();
                return;   // rooted while channeling
            }
        }
        else if (!isCharging && !healLatch && healAction.IsPressed() && CanHeal())
        {
            isHealing = true;
            healHeld = 0f;
            attackBufferLeft = 0f;
            return;
        }

        // ---- Attack (fresh press while free, or buffered from a hop) ----
        if (!isCharging && (attackPressed || attackBufferLeft > 0f))
        {
            attackBufferLeft = 0f;
            attackRoutine = StartCoroutine(Attack());
            return;
        }

        // ---- Dash charge: fresh press, or Space held through a move ----
        if (!isCharging &&
            dashQueued &&
            dashCooldownLeft <= 0f)
        {
            dashQueued = false;
            dashBufferLeft = 0f;

            isCharging = true;
            chargeHeld = 0f;
            lastChargeTick = 0;

            playerAnimator.SetBool("Charging", true);
        }

        if (isCharging)
        {
            if (dashAction.IsPressed())
            {
                chargeHeld += Time.deltaTime;
                UpdateChargeVisual();
                UpdateChargeTicks();
            }
            else
            {
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

    private int HealCost =>
    PlayerProgression.Instance ? PlayerProgression.Instance.CostFromFraction(healXpCostFraction) : int.MaxValue;

    private bool CanHeal() =>
        !IsDead && Health < maxHealth &&
        PlayerProgression.Instance != null &&
        PlayerProgression.Instance.XP >= HealCost;

    private void CompleteHeal()
    {
        // XP is only spent here, so cancelling early costs nothing
        if (PlayerProgression.Instance.TrySpendXP(HealCost))
        {
            AudioManager.Instance.PlaySound(healSFX, 1f);
            Health = Mathf.Min(maxHealth, Health + healAmount);
            onHealthChanged?.Invoke(Health, maxHealth);
            onHealed?.Invoke();
        }
        healLatch = true;   // release J, then press again for another heal
        CancelHeal();
    }

    private void CancelHeal()
    {
        playerAnimator.SetBool("isHealing", false);
        isHealing = false;
        healHeld = 0f;
    }

    // ---------- Attack ----------

    private IEnumerator Attack()
    {
        isAttacking = true;
        attackBufferLeft = 0f;

        AudioManager.Instance.PlaySound(slashSFX, 0.5f);

        Vector2Int dir = new Vector2Int(facingX, 0);   // horizontal only
        Vector2Int target = Cell + dir;

        if (sprite) sprite.flipX = facingX < 0;

        playerAnimator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackStartup);

        // Active: hit whatever is on the square in front

        if (attackEffectPrefab)
        {
            Instantiate(
                attackEffectPrefab,
                board.CellToWorld(target, transform.position.z),
                Quaternion.identity
            );
        }

        var alreadyHit = new HashSet<Collider2D>();

        float t = 0f;

        do
        {
            HitCell(target, alreadyHit);

            t += Time.deltaTime;

            yield return null;

        } while (t < attackActive);

        // Recovery

        yield return new WaitForSeconds(attackRecovery);

        isAttacking = false;
        attackRoutine = null;
    }

    private void HitCell(
        Vector2Int c,
        HashSet<Collider2D> alreadyHit)
    {
        Vector2 size = Vector2.one * board.cellSize * 0.8f;

        var hits = Physics2D.OverlapBoxAll(
            board.CellToWorld(c),
            size,
            0f,
            enemyMask
        );

        foreach (var h in hits)
        {
            if (alreadyHit.Add(h) &&
                h.TryGetComponent<IDamageable>(out var d))
            {
                d.Hit(transform.position, attackDamage);
            }
        }
    }

    private void CancelAttack()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = null;
        isAttacking = false;
    }

    // ---------- Dash charge visuals ----------

    private void UpdateChargeVisual()
    {
        if (!sprite) return;

        // Don't flash at all until fully charged
        if (chargeHeld < dashChargeTime)
            return;

        // Pulse between minimum and maximum opacity
        float flashAmount = Mathf.Lerp(
            minFlashOpacity,
            maxFlashOpacity,
            Mathf.PingPong(Time.time * readyPulseSpeed, 1f)
        );

        sprite.GetPropertyBlock(flashProperties);

        flashProperties.SetFloat("_FlashAmount", flashAmount);

        sprite.SetPropertyBlock(flashProperties);
    }

    private void CancelCharge()
    {
        isCharging = false;
        chargeHeld = 0f;

        // Reset white flash

        if (sprite)
        {
            sprite.GetPropertyBlock(flashProperties);

            flashProperties.SetFloat(
                "_FlashAmount",
                0f
            );

            sprite.SetPropertyBlock(flashProperties);
        }

        playerAnimator.SetBool("Charging", false);
    }

    private void UpdateChargeTicks()
    {
        int tick = Mathf.FloorToInt(ChargeProgress * 3f);

        tick = Mathf.Min(tick, 3);

        if (tick > lastChargeTick)
        {
            lastChargeTick = tick;

            if (dashTickSFX)
                AudioManager.Instance.PlaySound(dashTickSFX, 0.5f);
        }
    }

    // ---------- Movement ----------

    private void TryStep(Vector2Int dir)
    {
        Vector2Int target = Cell + dir;

        if (board.IsBlocked(target))
            return;

        StartCoroutine(
            MoveTo(target, stepTime, false)
        );
    }

    private bool TryDash()
    {
        Vector2Int dir =
            inputDir != Vector2Int.zero
                ? inputDir
                : lastDir;

        // Go as far as possible up to dashDistance

        int n = 0;

        for (int i = 1; i <= dashDistance; i++)
        {
            if (board.IsBlocked(Cell + dir * i))
                break;

            n = i;
        }

        if (n == 0)
            return false;

        dashCooldownLeft = dashCooldown;

        StartCoroutine(MoveTo(Cell + dir * n, dashTime, true));

        AudioManager.Instance.PlaySound(dashSlashSFX, 0.5f);

        return true;
    }

    private IEnumerator MoveTo(
        Vector2Int target,
        float duration,
        bool dash)
    {
        CancelCharge();

        isMoving = true;
        IsDashing = dash;

        Vector3 start = transform.position;

        Vector3 end = board.CellToWorld(
            target,
            start.z
        );

        Vector2Int from = Cell;
        Cell = target;

        // Only normal movement plays Hop

        if (!dash)
        {
            root.ResetTrigger("HopEnd");
            root.SetTrigger("Hop");
        }


        // ...
        if (dash)
        {
            Slash(start, end);
            onDashed?.Invoke(from, target);
        }

        for (
            float t = 0f;
            t < 1f;
            t += Time.deltaTime / duration
        )
        {
            float e = dash
                ? 1f - (1f - t) * (1f - t)
                : t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(
                start,
                end,
                e
            );

            yield return null;
        }

        transform.position = end;

        // Only normal movement ends Hop

        if (!dash)
            root.SetTrigger("HopEnd");

        IsDashing = false;
        isMoving = false;

        stepCooldownLeft = stepCooldown;
    }

    // ---------- Dash attack ----------

    private void Slash(Vector3 a, Vector3 b)
    {
        float radius = board.cellSize * 0.4f;



        var hits = Physics2D.CircleCastAll(
            a,
            radius,
            (b - a).normalized,
            Vector3.Distance(a, b),
            enemyMask
        );

        foreach (var h in hits)
        {
            if (h.collider.TryGetComponent<IDamageable>(out var d))
                d.Hit(a, dashDamage);
        }
    }

    // ---------- Health ----------

    public void AddMaxHealth(int amount)
    {
        maxHealth += amount;
        Health += amount;
        onHealthChanged?.Invoke(Health, maxHealth);
    }

    public void TakeHit()
    {
        if (IsDead || IsDashing || IsInvincible)
            return;

        Health--;

        CancelCharge();
        CancelAttack();
        CancelHeal();

        attackBufferLeft = 0f;
        dashBufferLeft = 0f;
        dashQueued = false;

        onHit?.Invoke();

        onHealthChanged?.Invoke(Health, maxHealth);

        if (IsDead)
        {
            Die();
            return;
        }

        StartCoroutine(Invincibility());
    }

    private void CheckContact()
    {
        if (IsDashing || IsInvincible)
            return;

        if (Physics2D.OverlapCircle(
            transform.position,
            board.cellSize * contactRadius,
            enemyMask))
        {
            TakeHit();
        }
    }

    private IEnumerator Invincibility()
    {
        invincibleLeft = invincibilityTime;

        while (invincibleLeft > 0f)
        {
            if (sprite)
                sprite.enabled = !sprite.enabled;

            yield return new WaitForSeconds(blinkInterval);
        }

        if (sprite)
            sprite.enabled = true;
    }

    private void Die()
    {
        if (sprite)
            sprite.enabled = true;

        Debug.Log("Player died!");

        onDeath?.Invoke();
    }
}
