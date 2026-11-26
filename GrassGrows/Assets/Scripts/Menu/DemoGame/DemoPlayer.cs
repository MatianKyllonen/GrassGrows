using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoPlayer : MonoBehaviour
{
    public DemoBoard board;

    [Header("Step")]
    public float stepTime = 0.10f;
    public float hopClipLength = 0.3f;

    [Header("Dash")]
    public int dashDistance = 3;
    public float dashTime = 0.12f;
    [Tooltip("Shorter than the real game so the menu stays lively")]
    public float chargeTime = 0.7f;
    public float dashCooldown = 1.2f;
    [Range(0f, 1f)] public float dashChance = 0.8f;
    public float readyPulseSpeed = 5f;
    [Range(0f, 1f)] public float minFlash = 0.2f;
    [Range(0f, 1f)] public float maxFlash = 0.65f;

    [Header("Dash Afterimage")]
    [Range(0f, 1f)] public float dashAfterimageOpacity = 0.25f;
    public float dashAfterimageLifetime = 0.15f;
    public float dashAfterimageInterval = 0.025f;

    [Header("Attack")]
    public float attackStartup = 0.12f;
    public float attackActive = 0.06f;
    public float attackRecovery = 0.25f;
    public GameObject attackEffectPrefab;

    [Header("Behaviour")]
    public Vector2 thinkTime = new Vector2(0.08f, 0.25f);
    [Range(0f, 1f)] public float randomStepChance = 0.15f;

    [Header("Visuals")]
    public SpriteRenderer sprite;
    public Animator root;
    public Animator playerAnimator;
    public float hitBlinkTime = 0.6f;
    public float blinkInterval = 0.08f;

    public Vector2Int Cell { get; private set; }

    private int facingX = 1;
    private float nextDashTime;
    private MaterialPropertyBlock mpb;
    private Coroutine blinkRoutine;

    private static readonly int FlashID = Shader.PropertyToID("_FlashAmount");
    private static readonly Vector2Int[] Dirs =
        { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    private void Awake() => mpb = new MaterialPropertyBlock();

    private void Start()
    {
        if (!board) board = DemoBoard.Instance;
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        board.player = this;

        Cell = board.WorldToCell(transform.position);
        transform.position = board.CellToWorld(Cell, transform.position.z);

        DemoAnim.Float(root, "HopSpeed", hopClipLength / stepTime);
        SetFlash(0f);
        StartCoroutine(Brain());
    }

    // ---------- Brain ----------

    private IEnumerator Brain()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(thinkTime.x, thinkTime.y));

            // 1. Swing at a pawn right beside us
            int side = 0;
            if (board.PawnAt(Cell + Vector2Int.right)) side = 1;
            else if (board.PawnAt(Cell + Vector2Int.left)) side = -1;
            if (side != 0) { yield return Attack(side); continue; }

            // 2. Dash through pawns lined up with us
            if (Time.time >= nextDashTime && Random.value < dashChance)
            {
                Vector2Int dir = BestDashDir(out int count);
                if (count > 0) { yield return ChargeAndDash(dir); continue; }
            }

            // 3. Otherwise reposition
            Vector2Int step = ChooseStep();
            if (step != Vector2Int.zero) yield return Step(step);
        }
    }

    // ---------- Decisions ----------

    private DemoPawn Nearest()
    {
        DemoPawn best = null;
        int bestDist = int.MaxValue;
        foreach (var p in board.pawns)
        {
            if (!board.InBounds(p.Cell)) continue;   // still walking in from off-board
            Vector2Int d = p.Cell - Cell;
            int m = Mathf.Abs(d.x) + Mathf.Abs(d.y);
            if (m < bestDist) { bestDist = m; best = p; }
        }
        return best;
    }

    private Vector2Int BestDashDir(out int bestCount)
    {
        bestCount = 0;
        Vector2Int best = Vector2Int.zero;
        foreach (var d in Dirs)
        {
            int count = 0;
            for (int i = 1; i <= dashDistance; i++)
            {
                Vector2Int c = Cell + d * i;
                if (!board.InBounds(c)) break;
                if (board.PawnAt(c)) count++;
            }
            if (count > bestCount) { bestCount = count; best = d; }
        }
        return best;
    }

    private Vector2Int ChooseStep()
    {
        // Squares a pawn could capture on next
        var danger = new HashSet<Vector2Int>();
        foreach (var p in board.pawns)
            if (board.InBounds(p.Cell))
                foreach (var c in p.DangerCells()) danger.Add(c);

        var safe = new List<Vector2Int>();
        var any = new List<Vector2Int>();
        foreach (var d in Dirs)
        {
            Vector2Int c = Cell + d;
            if (!board.InBounds(c) || board.PawnAt(c)) continue;
            any.Add(d);
            if (!danger.Contains(c)) safe.Add(d);
        }

        var options = safe.Count > 0 ? safe : any;
        if (options.Count == 0) return Vector2Int.zero;

        DemoPawn target = Nearest();
        if (!target || Random.value < randomStepChance)
            return options[Random.Range(0, options.Count)];

        // Aim for the square beside the pawn, so the swing is head-on (pawns can't capture that)
        Vector2Int goal = target.Cell;
        int goalDist = int.MaxValue;
        foreach (var off in new[] { Vector2Int.left, Vector2Int.right })
        {
            Vector2Int g = target.Cell + off;
            if (!board.InBounds(g) || board.PawnAt(g)) continue;
            int m = Mathf.Abs(g.x - Cell.x) + Mathf.Abs(g.y - Cell.y);
            if (m < goalDist) { goalDist = m; goal = g; }
        }

        if (Cell == goal) return Vector2Int.zero;   // in position, wait for the pawn

        int best = int.MaxValue;
        var picks = new List<Vector2Int>();
        foreach (var d in options)
        {
            Vector2Int c = Cell + d;
            int m = Mathf.Abs(goal.x - c.x) + Mathf.Abs(goal.y - c.y);
            if (m < best) { best = m; picks.Clear(); picks.Add(d); }
            else if (m == best) picks.Add(d);
        }
        return picks[Random.Range(0, picks.Count)];
    }

    // ---------- Actions ----------

    private IEnumerator Step(Vector2Int dir)
    {
        if (dir.x != 0) Face(dir.x);
        yield return Move(Cell + dir, stepTime, false);
    }

    private IEnumerator Attack(int side)
    {
        Face(side);
        DemoAnim.Trigger(playerAnimator, "Attack");

        yield return new WaitForSeconds(attackStartup);

        Vector2Int target = Cell + new Vector2Int(side, 0);
        if (attackEffectPrefab)
            Destroy(Instantiate(attackEffectPrefab, board.CellToWorld(target, transform.position.z), Quaternion.identity), 2f);

        float t = 0f;
        do
        {
            var p = board.PawnAt(target);
            if (p) p.Kill();
            t += Time.deltaTime;
            yield return null;
        } while (t < attackActive);

        yield return new WaitForSeconds(attackRecovery);
    }


    private IEnumerator ChargeAndDash(Vector2Int dir)
    {
        if (dir.x != 0)
            Face(dir.x);

        DemoAnim.Bool(playerAnimator, "Charging", true);

        float held = 0f;

        while (held < chargeTime + 0.15f)
        {
            held += Time.deltaTime;

            if (held >= chargeTime)
            {
                SetFlash(Mathf.Lerp(
                    minFlash,
                    maxFlash,
                    Mathf.PingPong(Time.time * readyPulseSpeed, 1f)
                ));
            }

            yield return null;
        }

        SetFlash(0f);
        DemoAnim.Bool(playerAnimator, "Charging", false);

        nextDashTime = Time.time + dashCooldown;

        int reach = dashDistance;

        // Don't dash outside the board.
        for (int i = 1; i <= dashDistance; i++)
        {
            if (!board.InBounds(Cell + dir * i))
            {
                reach = i - 1;
                break;
            }
        }

        if (reach > 0)
            yield return Move(Cell + dir * reach, dashTime, true);
    }


    private IEnumerator Move(Vector2Int target, float duration, bool dash)
    {
        Vector3 a = transform.position;
        Vector3 b = board.CellToWorld(target, a.z);

        Vector2Int from = Cell;
        Cell = target;

        if (!dash)
        {
            DemoAnim.ResetTrigger(root, "HopEnd");
            DemoAnim.Trigger(root, "Hop");
        }

        // Dash bookkeeping: squares are processed as the player reaches them
        Vector2Int dashStep = Vector2Int.zero;
        int dashCells = 0;
        int nextCell = 0;

        if (dash)
        {
            Vector2Int delta = target - from;
            dashStep = new Vector2Int(System.Math.Sign(delta.x), System.Math.Sign(delta.y));
            dashCells = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
        }

        float afterimageTimer = dashAfterimageInterval;   // start full so the first frame leaves an image

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float e = dash ? 1f - (1f - t) * (1f - t) : t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(a, b, e);

            if (dash)
            {
                afterimageTimer += Time.deltaTime;
                if (afterimageTimer >= dashAfterimageInterval)
                {
                    CreateDashAfterimage();
                    afterimageTimer = 0f;
                }

                // Cut each square's pawn only once the player has reached it
                float travelled = Vector3.Distance(a, transform.position) / board.cellSize;
                while (nextCell <= dashCells &&
                       travelled >= (nextCell == 0 ? 0.5f : nextCell))
                {
                    DashPassCell(from + dashStep * nextCell);
                    nextCell++;
                }
            }

            yield return null;
        }

        transform.position = b;

        if (dash)
        {
            while (nextCell <= dashCells)
            {
                DashPassCell(from + dashStep * nextCell);
                nextCell++;
            }
        }

        if (!dash) DemoAnim.Trigger(root, "HopEnd");
    }

    private void DashPassCell(Vector2Int c)
    {
        var p = board.PawnAt(c);
        if (p) p.Kill();
    }

    private void CreateDashAfterimage()
    {
        if (!sprite || !sprite.sprite) return;

        var go = new GameObject("Dash Afterimage");
        go.transform.position = sprite.transform.position;
        go.transform.rotation = sprite.transform.rotation;
        go.transform.localScale = sprite.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite.sprite;
        sr.flipX = sprite.flipX;
        sr.flipY = sprite.flipY;
        sr.sortingLayerID = sprite.sortingLayerID;
        sr.sortingOrder = sprite.sortingOrder - 1;
        sr.sharedMaterial = sprite.sharedMaterial;
        sr.maskInteraction = sprite.maskInteraction;

        Color c = sprite.color;
        c.a *= dashAfterimageOpacity;
        sr.color = c;

        StartCoroutine(FadeAfterimage(sr, dashAfterimageLifetime));
    }

    private IEnumerator FadeAfterimage(SpriteRenderer sr, float duration)
    {
        if (duration <= 0f) { Destroy(sr.gameObject); yield break; }

        Color start = sr.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (!sr) yield break;
            Color c = start;
            c.a = Mathf.Lerp(start.a, 0f, t / duration);
            sr.color = c;
            yield return null;
        }

        if (sr) Destroy(sr.gameObject);
    }

    // ---------- Visuals ----------

    private void Face(int x)
    {
        facingX = x;
        if (sprite) sprite.flipX = x < 0;
    }

    private void SetFlash(float v)
    {
        if (!sprite) return;
        sprite.GetPropertyBlock(mpb);
        mpb.SetFloat(FlashID, v);
        sprite.SetPropertyBlock(mpb);
    }

    // Pawns call this on a capture. Purely cosmetic, the demo player can't die.
    public void FakeHit()
    {
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        for (float t = 0f; t < hitBlinkTime; t += blinkInterval)
        {
            if (sprite) sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
        if (sprite) sprite.enabled = true;
        blinkRoutine = null;
    }
}