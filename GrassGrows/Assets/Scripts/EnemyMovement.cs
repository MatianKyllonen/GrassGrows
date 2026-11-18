using System.Collections;
using UnityEngine;

public abstract class EnemyMovement : MonoBehaviour
{
    public BoardGrid board;
    public PlayerController player;
    public LayerMask enemyMask;                       // so enemies don't stack
    public Vector2Int forward = Vector2Int.left;      // enemies come from the right

    protected Vector2Int cell;

    // Perpendicular to forward: (0,1) when marching left/right, (1,0) when marching up/down
    protected Vector2Int Side => new Vector2Int(forward.y != 0 ? 1 : 0, forward.x != 0 ? 1 : 0);

    protected virtual void Start()
    {
        board = GameManager.instance.board;
        player = GameManager.instance.player;

        cell = board.WorldToCell(transform.position);
        transform.position = board.CellToWorld(cell, transform.position.z);
        StartCoroutine(Brain());
    }

    protected abstract IEnumerator Brain();

    protected bool CanEnter(Vector2Int c)
    {
        if (board.IsBlocked(c)) return false;
        foreach (var h in Physics2D.OverlapPointAll(board.CellToWorld(c), enemyMask))
            if (h.gameObject != gameObject) return false;
        return true;
    }

    protected IEnumerator Move(Vector2Int target, float duration)
    {
        Vector3 start = transform.position;
        Vector3 end = board.CellToWorld(target, start.z);
        cell = target;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        transform.position = end;
    }
}