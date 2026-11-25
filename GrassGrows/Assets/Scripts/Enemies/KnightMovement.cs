using System.Collections;
using UnityEngine;

public class KnightMovement : EnemyMovement
{
    [Header("Timing")]
    public float rest = 1.2f;
    [Tooltip("Marker is shown on the landing square for this long. The player can dodge inside this window.")]
    public float windup = 0.6f;
    public float blinkDelay = 0.06f;    // pause on each square it hops over
    public float recovery = 0.3f;       // vulnerable time after reappearing

    [Header("Effects")]
    public GameObject smokePrefab;      // puff spawned on every square it blinks through
    public GameObject landingMarker;    // optional telegraph on the landing square
    public float smokeLifetime = 1.5f;
    public AudioClip smokeSFX;

    private Collider2D[] colliders;

    private static readonly Vector2Int[] Jumps =
    {
        new Vector2Int(1, 2),  new Vector2Int(2, 1),  new Vector2Int(2, -1), new Vector2Int(1, -2),
        new Vector2Int(-1, -2), new Vector2Int(-2, -1), new Vector2Int(-2, 1), new Vector2Int(-1, 2)
    };

    protected override void Start()
    {
        colliders = GetComponentsInChildren<Collider2D>();
        base.Start();
    }

    protected override IEnumerator Brain()
    {
        yield return new WaitForSeconds(Random.Range(0f, rest));   // desync

        while (true)
        {
            yield return new WaitForSeconds(rest);
            if (player.IsDead) yield break;

            // Commit: the landing square is chosen from where the player is RIGHT NOW
            Vector2Int landing = ChooseLanding();
            if (landing == cell) continue;

            // Telegraph
            Color original = sprite ? sprite.color : Color.white;
            if (sprite) sprite.color = Color.red;
            if (landingMarker)
            {
                var m = Instantiate(landingMarker, board.CellToWorld(landing, transform.position.z), Quaternion.identity);
                Destroy(m, windup);
            }

            yield return new WaitForSeconds(windup);
            if (sprite) sprite.color = original;
            if (player.IsDead) yield break;

            // The square could have been taken during the windup
            if (!CanEnter(landing)) continue;

            yield return SmokeBlink(landing);
            if (player.IsDead) yield break;

            yield return new WaitForSeconds(recovery);
        }
    }

    // Best L-jump toward the player's current cell. Landing exactly on the player is allowed.
    private Vector2Int ChooseLanding()
    {
        Vector2Int aim = player.Cell;
        int best = int.MaxValue;
        Vector2Int pick = cell;
        int ties = 0;

        foreach (var j in Jumps)
        {
            Vector2Int c = cell + j;
            if (!CanEnter(c)) continue;   // in bounds, no walls, no other enemies

            int d = (aim - c).sqrMagnitude;
            if (d < best) { best = d; pick = c; ties = 1; }
            else if (d == best && Random.Range(0, ++ties) == 0) pick = c;   // random among ties
        }
        return pick;
    }

    private IEnumerator SmokeBlink(Vector2Int landing)
    {
        Vector2Int origin = cell;
        Vector2Int delta = landing - origin;

        // The squares it hops over: two along the long axis, then the landing square
        Vector2Int longStep = Mathf.Abs(delta.x) == 2
            ? new Vector2Int((int)Mathf.Sign(delta.x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(delta.y));

        Vector2Int[] path = { origin + longStep, origin + longStep * 2, landing };

        cell = landing;   // claim the square now so no other enemy picks it

        // Vanish: smoke bomb at the start, can't be hit or touched while gone
        Smoke(origin);
        SetVisible(false);
        yield return new WaitForSeconds(blinkDelay);

        // Blink through each square it hops over
        for (int i = 0; i < path.Length; i++)
        {
            transform.position = board.CellToWorld(path[i], transform.position.z);
            Smoke(path[i]);
            if (i < path.Length - 1)
                yield return new WaitForSeconds(blinkDelay);
        }

        // Reappear
        SetVisible(true);
        FacePlayer();

        // Strike only if the player is still standing there
        if (player.Cell == cell)
            player.TakeHit();
    }

    private void SetVisible(bool visible)
    {
        if (sprite) sprite.enabled = visible;
        foreach (var c in colliders)
            if (c) c.enabled = visible;
    }

    private void Smoke(Vector2Int c)
    {
        if (smokePrefab)
        {
            var go = Instantiate(smokePrefab, board.CellToWorld(c, transform.position.z), Quaternion.identity);
            Destroy(go, smokeLifetime);
        }
        if (smokeSFX && AudioManager.Instance)
            AudioManager.Instance.PlaySound(smokeSFX, 0.4f);
    }
}