using System.Collections;
using UnityEngine;

public class PawnMovement : EnemyMovement
{
    public float stepInterval = 0.9f;
    public float moveTime = 0.2f;

    protected override IEnumerator Brain()
    {
        yield return new WaitForSeconds(Random.Range(0f, stepInterval)); // desync pawns
        while (true)
        {
            yield return new WaitForSeconds(stepInterval);
            if (player.IsDead) yield break;

            forward = DirToPlayer();

            // Capture diagonally forward
            Vector2Int diagA = cell + forward + Side;
            Vector2Int diagB = cell + forward - Side;
            if (player.Cell == diagA) { yield return Move(diagA, moveTime); player.TakeHit(); continue; }
            if (player.Cell == diagB) { yield return Move(diagB, moveTime); player.TakeHit(); continue; }

            Vector2Int next = cell + forward;

            // Player is directly in front: pawns can't capture head-on, so sidestep
            // and capture diagonally next turn
            if (next == player.Cell)
            {
                Vector2Int s = Random.value < 0.5f ? Side : -Side;
                if (CanEnter(cell + s) && player.Cell != cell + s)
                    yield return Move(cell + s, moveTime);
                continue;
            }

            if (CanEnter(next)) { yield return Move(next, moveTime); continue; }

            // Blocked by a wall or another enemy: try the other axis toward the player
            Vector2Int d = player.Cell - cell;
            Vector2Int alt = forward.x != 0
                ? new Vector2Int(0, d.y != 0 ? (int)Mathf.Sign(d.y) : 0)
                : new Vector2Int(d.x != 0 ? (int)Mathf.Sign(d.x) : 0, 0);
            if (alt != Vector2Int.zero && CanEnter(cell + alt) && player.Cell != cell + alt)
                yield return Move(cell + alt, moveTime);
        }
    }
}