using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IKnockbackable
{
    void Knockback(Vector2Int direction, int distance);
}

public abstract class EnemyMovement : MonoBehaviour, IKnockbackable
{
    public BoardGrid board;
    public PlayerController player;
    public LayerMask enemyMask;
    public Animator root;

    protected SpriteRenderer sprite;

    [HideInInspector] public Vector2Int forward = Vector2Int.left;   // recalculated toward the player
    private static readonly List<EnemyMovement> all = new List<EnemyMovement>();
    protected virtual void OnEnable() => all.Add(this);
    protected virtual void OnDisable() => all.Remove(this);

    [Header("Charge")]
    [Tooltip("On: always travels the full range (old behaviour). Off: stops early once the next square would not bring it closer to the player.")]
    public bool forceFullRange = false;

    [Header("Knockback")]
    public float knockbackTime = 0.12f;     // how long the slide takes
    private float knockbackStun = 0.10f;     // time before the piece resumes its brain
    [Tooltip("Subtracted from incoming knockback distance. Use for heavy pieces.")]
    public int knockbackResistance = 0;

    private bool hasEnteredBoard;

    private Enemy enemy;
    private Color baseColor = Color.white;

    // Decided once, at commit time, from where the player is RIGHT NOW.
    // Returns how many squares to travel along `direction`.
    protected int PlanSteps(Vector2Int direction, int range)
    {
        if (forceFullRange) return range;

        Vector2Int aim = player.Cell;                 // snapshot, never re-read
        int best = 0;
        int bestDist = (aim - cell).sqrMagnitude;

        Vector2Int c = cell;
        for (int i = 1; i <= range; i++)
        {
            c += direction;
            if (!CanEnter(c)) break;                  // don't plan through walls or enemies

            int d = (aim - c).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;                                  // 0 = no square gets closer, don't charge
    }

    // Executes the plan exactly. Only walls and other enemies can cut it short.
    protected IEnumerator Charge(Vector2Int direction, int steps, float cellTime)
    {
        for (int i = 0; i < steps; i++)
        {
            Vector2Int next = cell + direction;
            if (!CanEnter(next)) break;

            yield return Move(next, cellTime);

            if (player.Cell == cell)
            {
                player.TakeHit();
                break;
            }

            if (player.IsDead)
                yield break;
        }
    }

    // Axis direction toward the player (the longer axis wins)
    protected Vector2Int DirToPlayer()
    {
        Vector2Int d = player.Cell - cell;
        if (d == Vector2Int.zero) return forward;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return new Vector2Int((int)Mathf.Sign(d.x), 0);
        return new Vector2Int(0, (int)Mathf.Sign(d.y));
    }

    protected Vector2Int cell;

    // Perpendicular to forward: (0,1) when marching left/right, (1,0) when marching up/down
    protected Vector2Int Side => new Vector2Int(forward.y != 0 ? 1 : 0, forward.x != 0 ? 1 : 0);

    protected virtual void Start()
    {
        board = GameManager.instance.board;
        player = GameManager.instance.player;
        sprite = GetComponentInChildren<SpriteRenderer>();

        cell = board.WorldToCell(transform.position);
        hasEnteredBoard = IsInsideBoard(cell);
        transform.position = board.CellToWorld(cell, transform.position.z);
        StartCoroutine(Brain());
    }

    protected bool IsInsideBoard(Vector2Int c)
    {
        return c.x >= 0 && c.x < board.size.x &&
               c.y >= 0 && c.y < board.size.y;
    }

    protected abstract IEnumerator Brain();

    protected bool CanEnter(Vector2Int c)
    {
        bool insideBoard = IsInsideBoard(c);

        // Enemies that have entered the board cannot leave it.
        if (hasEnteredBoard && !insideBoard)
            return false;

        // Outside cells are allowed before entering the board.
        if (insideBoard && board.IsBlocked(c))
            return false;

        // Check squares claimed by other enemies.
        foreach (var e in all)
        {
            if (e != this && e.cell == c)
                return false;
        }

        // Check physical enemy colliders.
        foreach (var h in Physics2D.OverlapPointAll(board.CellToWorld(c), enemyMask))
        {
            if (h.gameObject != gameObject)
                return false;
        }

        return true;
    }

    protected void FacePlayer()
    {
        if (!sprite) return;

        Vector2Int direction = player.Cell - cell;

        if (direction.x != 0)
            sprite.flipX = direction.x < 0;
    }

    public void Knockback(Vector2Int direction, int distance)
    {
        if (enemy && enemy.IsDead) return;
        if (direction == Vector2Int.zero) return;

        distance -= knockbackResistance;
        if (distance <= 0) return;

        // Work out how far it can actually go
        Vector2Int target = cell;
        for (int i = 0; i < distance; i++)
        {
            Vector2Int next = target + direction;
            if (next == player.Cell || !CanEnter(next)) break;
            target = next;
        }

        // Interrupt whatever the piece was doing (windup, charge, hop)
        StopAllCoroutines();
        if (sprite) sprite.color = baseColor;   // clear the red telegraph
        if (root)
        {
            root.ResetControllerState();
        }

        StartCoroutine(KnockbackRoutine(target));
    }

    private IEnumerator KnockbackRoutine(Vector2Int target)
    {
        Vector3 start = transform.position;
        Vector3 end = board.CellToWorld(target, start.z);

        cell = target;

        if (IsInsideBoard(cell))
            hasEnteredBoard = true;
        for (float t = 0f; t < 1f; t += Time.deltaTime / knockbackTime)
        {
            float e = 1f - (1f - t) * (1f - t);   // ease-out
            transform.position = Vector3.Lerp(start, end, e);
            yield return null;
        }
        transform.position = end;

        yield return new WaitForSeconds(knockbackStun);
        StartCoroutine(Brain());   // resume with a fresh decision
    }



    protected IEnumerator Move(Vector2Int target, float duration)
    {
        FacePlayer();

        Vector3 start = transform.position;
        Vector3 end = board.CellToWorld(target, start.z);

        cell = target;

        if (IsInsideBoard(cell))
            hasEnteredBoard = true;

        root.SetTrigger("Hop");

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        root.SetTrigger("HopEnd");
        transform.position = end;
    }
}