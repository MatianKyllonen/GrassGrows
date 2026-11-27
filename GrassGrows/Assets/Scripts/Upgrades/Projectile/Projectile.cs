using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Tooltip("Squares per second")]
    public float speed = 14f;

    private BoardGrid board;
    private LayerMask mask;
    private Vector2Int dir, cell;
    private Vector3 targetPos;
    private int stepsLeft, arrivals, damage, pierceLeft;
    private readonly HashSet<Collider2D> hit = new HashSet<Collider2D>();
    private bool ready;

    public void Init(BoardGrid board, LayerMask mask, Vector2Int startCell, Vector2Int dir,
                     int range, int damage, int pierce)
    {
        this.board = board;
        this.mask = mask;
        this.dir = dir;
        this.damage = damage;
        pierceLeft = pierce;
        cell = startCell;
        stepsLeft = range + 1;   // +1 because the first square is the melee square, which is skipped

        transform.position = board.CellToWorld(startCell, transform.position.z);

        // Sprite is drawn facing right
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dir.x < 0 ? -1f : 1f);
        transform.localScale = s;

        ready = true;
        Advance();
    }

    private void Advance()
    {
        Vector2Int next = cell + dir;
        if (stepsLeft <= 0 || board.IsBlocked(next))   // out of range, wall or board edge
        {
            Destroy(gameObject);
            return;
        }

        cell = next;
        stepsLeft--;
        targetPos = board.CellToWorld(cell, transform.position.z);
    }

    private void Update()
    {
        if (!ready) return;

        transform.position = Vector3.MoveTowards(
            transform.position, targetPos, speed * board.cellSize * Time.deltaTime);

        if ((transform.position - targetPos).sqrMagnitude > 0.0001f) return;

        // Arrived on a square
        arrivals++;
        if (arrivals > 1 && HitCell())   // arrival 1 is the melee square
        {
            Destroy(gameObject);
            return;
        }
        Advance();
    }

    // Returns true when the projectile has used up its pierce
    private bool HitCell()
    {
        var cols = Physics2D.OverlapBoxAll(
            board.CellToWorld(cell), Vector2.one * board.cellSize * 0.8f, 0f, mask);

        foreach (var c in cols)
        {
            if (!hit.Add(c) || !c.TryGetComponent<IDamageable>(out var d)) continue;

            d.Hit(transform.position, damage);
            if (pierceLeft-- <= 0) return true;
        }
        return false;
    }
}