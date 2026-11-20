using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BishopMovement : EnemyMovement
{
    public int range = 6;
    public float rest = 1.4f;
    public float windup = 0.6f;
    public float cellTime = 0.05f;

    private SpriteRenderer sr;
    private Color baseColor;

    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(-1, -1),
        new Vector2Int(-1,  1),
        new Vector2Int( 1, -1),
        new Vector2Int( 1,  1)
    };

    protected override void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) baseColor = sr.color;

        base.Start();
    }

    protected override IEnumerator Brain()
    {
        while (true)
        {
            yield return new WaitForSeconds(rest);

            if (player.IsDead) yield break;

            Vector2Int dir = PickDirection();

            if (dir == Vector2Int.zero)
                continue;

            if (sr) sr.color = Color.red;

            yield return new WaitForSeconds(windup);

            if (sr) sr.color = baseColor;

            for (int i = 0; i < range; i++)
            {
                Vector2Int next = cell + dir;

                if (!CanEnter(next))
                    break;

                yield return Move(next, cellTime);

                if (player.Cell == cell)
                    player.TakeHit();
            }
        }
    }

    private Vector2Int PickDirection()
    {
        List<Vector2Int> closing = new List<Vector2Int>();

        Vector2Int toPlayer = player.Cell - cell;

        foreach (var d in Dirs)
        {
            if (!CanEnter(cell + d))
                continue;

            // Is the player somewhere along this diagonal?
            for (int i = 1; i <= range; i++)
            {
                if (cell + d * i == player.Cell)
                    return d;
            }

            // Does this direction reduce our distance?
            if (toPlayer.x * d.x > 0 &&
                toPlayer.y * d.y > 0)
            {
                closing.Add(d);
            }
        }

        return closing.Count > 0
            ? closing[Random.Range(0, closing.Count)]
            : Vector2Int.zero;
    }
}