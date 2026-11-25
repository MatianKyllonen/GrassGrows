using System.Collections;
using UnityEngine;

public class DemoPawn : MonoBehaviour
{
    public float stepInterval = 0.9f;
    public float moveTime = 0.2f;
    public float reactionDelay = 0.35f;
    public float hopClipLength = 0.3f;

    public SpriteRenderer sprite;
    public Animator root;
    public GameObject deathEffect;

    public Vector2Int Cell { get; private set; }

    private DemoBoard board;
    private bool dead;

    // ---------- Setup ----------

    // The spawner calls this right after Instantiate. Start() covers pawns placed in the scene by hand.
    public void Init(DemoBoard b)
    {
        if (board) return;
        board = b;
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();

        Cell = board.WorldToCell(transform.position);
        transform.position = board.CellToWorld(Cell, transform.position.z);
        board.pawns.Add(this);

        DemoAnim.Float(root, "HopSpeed", hopClipLength / moveTime);
        StartCoroutine(Brain());
    }

    private void Start() => Init(DemoBoard.Instance);

    // ---------- Helpers ----------

    public Vector2Int Forward
    {
        get
        {
            if (!board.player) return Vector2Int.left;
            Vector2Int d = board.player.Cell - Cell;
            if (d == Vector2Int.zero) return Vector2Int.left;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return new Vector2Int((int)Mathf.Sign(d.x), 0);
            return new Vector2Int(0, (int)Mathf.Sign(d.y));
        }
    }

    private static Vector2Int SideOf(Vector2Int fwd) =>
        new Vector2Int(fwd.y != 0 ? 1 : 0, fwd.x != 0 ? 1 : 0);

    // The two squares this pawn can capture on. The demo player steers around them.
    public Vector2Int[] DangerCells()
    {
        Vector2Int f = Forward, s = SideOf(f);
        return new[] { Cell + f + s, Cell + f - s };
    }

    private bool Free(Vector2Int c) => board.InBounds(c) && board.PawnAt(c, this) == null;

    // ---------- Brain ----------

    private IEnumerator Brain()
    {
        yield return new WaitForSeconds(Random.Range(0f, stepInterval));

        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, stepInterval - reactionDelay));
            if (!board.player) continue;

            Vector2Int intent = ChooseTarget();               // decided now...
            yield return new WaitForSeconds(reactionDelay);   // ...executed later

            if (!board.player || intent == Cell || !Free(intent)) continue;

            bool diagonal = intent.x != Cell.x && intent.y != Cell.y;
            if (!diagonal && intent == board.player.Cell) continue;   // never capture head-on

            yield return Move(intent);

            if (diagonal && board.player.Cell == Cell)
                board.player.FakeHit();
        }
    }

    private Vector2Int ChooseTarget()
    {
        Vector2Int fwd = Forward, side = SideOf(fwd), p = board.player.Cell;

        Vector2Int dA = Cell + fwd + side, dB = Cell + fwd - side;
        if (p == dA && Free(dA)) return dA;
        if (p == dB && Free(dB)) return dB;

        Vector2Int next = Cell + fwd;

        // Player directly ahead: sidestep so a diagonal capture is possible later
        if (next == p)
        {
            Vector2Int s = Random.value < 0.5f ? side : -side;
            if (Free(Cell + s) && Cell + s != p) return Cell + s;
            if (Free(Cell - s) && Cell - s != p) return Cell - s;
            return Cell;
        }

        if (Free(next)) return next;

        // Blocked: try the other axis
        Vector2Int d = p - Cell;
        Vector2Int alt = fwd.x != 0
            ? new Vector2Int(0, d.y != 0 ? (int)Mathf.Sign(d.y) : 0)
            : new Vector2Int(d.x != 0 ? (int)Mathf.Sign(d.x) : 0, 0);
        if (alt != Vector2Int.zero && Free(Cell + alt) && Cell + alt != p) return Cell + alt;

        return Cell;
    }

    private IEnumerator Move(Vector2Int target)
    {
        Vector3 a = transform.position;
        Vector3 b = board.CellToWorld(target, a.z);
        Cell = target;   // claim the square immediately

        if (sprite && board.player)
        {
            int dx = board.player.Cell.x - Cell.x;
            if (dx != 0) sprite.flipX = dx < 0;
        }

        DemoAnim.ResetTrigger(root, "HopEnd");
        DemoAnim.Trigger(root, "Hop");

        for (float t = 0f; t < 1f; t += Time.deltaTime / moveTime)
        {
            transform.position = Vector3.Lerp(a, b, t);
            yield return null;
        }

        transform.position = b;
        DemoAnim.Trigger(root, "HopEnd");
    }

    public void Kill()
    {
        if (dead) return;
        dead = true;
        board.pawns.Remove(this);

        if (deathEffect)
            Destroy(Instantiate(deathEffect, transform.position, Quaternion.identity), 2f);

        Destroy(gameObject);
    }
}