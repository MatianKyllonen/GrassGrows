using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RookMovement : EnemyMovement
{
    public int range = 6;
    public float rest = 1.4f;
    public float windup = 0.6f;      // telegraph before the charge
    public float cellTime = 0.05f;   // time per square while charging

    private SpriteRenderer sr;
    private Color baseColor;

    private static readonly Vector2Int[] Dirs =
        { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

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
            if (dir == Vector2Int.zero) continue;   // boxed in, try again later

            if (sr) sr.color = Color.red;           // telegraph
            yield return new WaitForSeconds(windup);
            if (sr) sr.color = baseColor;

            for (int i = 0; i < range; i++)
            {
                Vector2Int next = cell + dir;
                if (!CanEnter(next)) break;
                yield return Move(next, cellTime);
                if (player.Cell == cell) player.TakeHit();
            }
        }
    }

    // Charge straight at the player if lined up, otherwise close the distance
    private Vector2Int PickDirection()
    {
        var closing = new List<Vector2Int>();
        foreach (var d in Dirs)
        {
            if (!CanEnter(cell + d)) continue;

            for (int i = 1; i <= range; i++)
                if (cell + d * i == player.Cell) return d;   // player in line

            // Does this direction bring us closer?
            if ((player.Cell - cell).x * d.x > 0 || (player.Cell - cell).y * d.y > 0)
                closing.Add(d);
        }
        return closing.Count > 0 ? closing[Random.Range(0, closing.Count)] : Vector2Int.zero;
    }
}