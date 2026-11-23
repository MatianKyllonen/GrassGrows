
using System.Collections;
using UnityEngine;

public class BishopMovement : EnemyMovement
{
    [Header("Movement")]
    public int range = 6;
    public float rest = 1.4f;
    public float windup = 0.6f;
    public float cellTime = 0.05f;

    protected override void Start()
    {
        RefreshHopSpeed();
        base.Start();
    }

    public void RefreshHopSpeed()
    {
        if (root && cellTime > 0f)
            root.SetFloat("HopSpeed", 0.3f / cellTime);
    }

    protected override IEnumerator Brain()
    {
        while (true)
        {
            // Rest before deciding where to move.
            yield return new WaitForSeconds(rest);

            if (player.IsDead)
                yield break;

            Vector2Int direction = PickDirection();

            if (direction == Vector2Int.zero)
                continue;

            // Telegraph the committed direction.
            Color originalColor = sprite ? sprite.color : Color.white;

            if (sprite)
                sprite.color = Color.red;

            yield return new WaitForSeconds(windup);

            if (sprite)
                sprite.color = originalColor;

            // Move one square at a time.
            for (int i = 0; i < range; i++)
            {
                Vector2Int next = cell + direction;

                if (!CanEnter(next))
                    break;

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
    }

    private Vector2Int PickDirection()
    {
        Vector2Int difference = player.Cell - cell;

        if (difference == Vector2Int.zero)
            return Vector2Int.zero;

        // Bishops always move diagonally.
        int dx = (int)Mathf.Sign(difference.x);
        int dy = (int)Mathf.Sign(difference.y);

        return new Vector2Int(dx, dy);
    }
}
