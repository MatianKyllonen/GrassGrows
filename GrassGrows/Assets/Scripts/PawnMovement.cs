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

            Vector2Int diagA = cell + forward + Side;
            Vector2Int diagB = cell + forward - Side;

            // Capture diagonally forward
            if (player.Cell == diagA)      { yield return Move(diagA, moveTime); player.TakeHit(); continue; }
            if (player.Cell == diagB)      { yield return Move(diagB, moveTime); player.TakeHit(); continue; }

            Vector2Int next = cell + forward;

            // Marched off the left edge of the board
            if (!board.InBounds(next)) { Destroy(gameObject); yield break; }

            if (CanEnter(next) && player.Cell != next)
                yield return Move(next, moveTime);
        }
    }
}