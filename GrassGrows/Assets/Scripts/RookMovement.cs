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

    // Rooks charge forward, up or down. If the player is lined up, they go for it.
    private Vector2Int PickDirection()
    {
        var options = new List<Vector2Int>();
        foreach (var d in new[] { forward, Side, -Side })
        {
            if (!CanEnter(cell + d)) continue;
            for (int i = 1; i <= range; i++)
                if (cell + d * i == player.Cell) return d;   // player in line
            options.Add(d);
        }
        return options.Count > 0 ? options[Random.Range(0, options.Count)] : Vector2Int.zero;
    }
}