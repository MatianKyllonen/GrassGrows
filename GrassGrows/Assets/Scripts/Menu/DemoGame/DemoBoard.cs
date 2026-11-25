using System.Collections.Generic;
using UnityEngine;

public class DemoBoard : MonoBehaviour
{
    public static DemoBoard Instance { get; private set; }

    public Vector2Int size = new Vector2Int(8, 8);
    public float cellSize = 1f;
    [Tooltip("World position of the CENTER of cell (0,0)")]
    public Vector2 origin = Vector2.zero;

    [HideInInspector] public DemoPlayer player;
    public readonly List<DemoPawn> pawns = new List<DemoPawn>();

    private void Awake() => Instance = this;

    public Vector2Int WorldToCell(Vector2 w) => new Vector2Int(
        Mathf.RoundToInt((w.x - origin.x) / cellSize),
        Mathf.RoundToInt((w.y - origin.y) / cellSize));

    public Vector3 CellToWorld(Vector2Int c, float z = 0f) =>
        new Vector3(origin.x + c.x * cellSize, origin.y + c.y * cellSize, z);

    public bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < size.x && c.y < size.y;

    public DemoPawn PawnAt(Vector2Int c, DemoPawn except = null)
    {
        foreach (var p in pawns)
            if (p != except && p.Cell == c) return p;
        return null;
    }
}