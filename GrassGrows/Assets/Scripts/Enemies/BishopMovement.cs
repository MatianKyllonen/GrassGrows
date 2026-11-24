
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

            // Commit: direction AND distance are locked in now
            int steps = PlanSteps(direction, range);
            if (steps == 0)
                continue;

            // Telegraph the committed move.
            Color originalColor = sprite ? sprite.color : Color.white;
            if (sprite) sprite.color = Color.red;
            yield return new WaitForSeconds(windup);
            if (sprite) sprite.color = originalColor;

            yield return Charge(direction, steps, cellTime);
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
