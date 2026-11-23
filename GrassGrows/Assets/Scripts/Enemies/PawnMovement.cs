using System.Collections;
using UnityEngine;

public class PawnMovement : EnemyMovement
{
    public float stepInterval = 0.9f;
    public float moveTime = 0.2f;
    [Tooltip("Gap between choosing a square and moving there. The player can dodge inside this window.")]
    public float reactionDelay = 0.35f;

    protected override void Start()
    {
        RefreshHopSpeed();
        base.Start();
    }

    public void RefreshHopSpeed()
    {
        if (root) root.SetFloat("HopSpeed", 0.3f / moveTime);
    }

    protected override IEnumerator Brain()
    {
        yield return new WaitForSeconds(Random.Range(0f, stepInterval)); // desync pawns

        while (true)
        {
            // Rest, then look at the player and commit
            yield return new WaitForSeconds(Mathf.Max(0f, stepInterval - reactionDelay));
            if (player.IsDead) yield break;

            Vector2Int intent = ChooseTarget();

            // Blind period: the player may move away, the pawn doesn't re-check
            yield return new WaitForSeconds(reactionDelay);
            if (player.IsDead) yield break;

            if (intent == cell) continue;

            bool diagonal = intent.x != cell.x && intent.y != cell.y;

            // Pawns never capture head-on
            if (!diagonal && intent == player.Cell) continue;

            // Never enter walls or other enemies
            if (!CanEnter(intent)) continue;

            yield return Move(intent, moveTime);

            // Only a diagonal capture hurts, and only if the player is actually there
            if (diagonal && player.Cell == cell) player.TakeHit();
        }
    }

    // Uses the player's position RIGHT NOW, but the move happens later
    private Vector2Int ChooseTarget()
    {
        forward = DirToPlayer();

        Vector2Int diagA = cell + forward + Side;
        Vector2Int diagB = cell + forward - Side;
        if (player.Cell == diagA) return diagA;   // commit to a capture
        if (player.Cell == diagB) return diagB;

        Vector2Int next = cell + forward;

        // Player directly in front: sidestep so a diagonal capture is possible later
        if (next == player.Cell)
        {
            Vector2Int s = Random.value < 0.5f ? Side : -Side;
            if (CanEnter(cell + s)) return cell + s;
            if (CanEnter(cell - s)) return cell - s;
            return cell;
        }

        if (CanEnter(next)) return next;

        // Blocked: try the other axis toward the player
        Vector2Int d = player.Cell - cell;
        Vector2Int alt = forward.x != 0
            ? new Vector2Int(0, d.y != 0 ? (int)Mathf.Sign(d.y) : 0)
            : new Vector2Int(d.x != 0 ? (int)Mathf.Sign(d.x) : 0, 0);
        if (alt != Vector2Int.zero && CanEnter(cell + alt)) return cell + alt;

        return cell;
    }
}