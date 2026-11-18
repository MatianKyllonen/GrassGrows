
using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int size = new Vector2Int(8, 8);
    public float cellSize = 1f;

    [Tooltip("World position of the CENTER of cell (0,0)")]
    public Vector2 origin = Vector2.zero;

    public bool drawGrid = true;
    [Header("Tile Settings")]
    public GameObject tilePrefab;
    public Color color1;
    public Color color2;
    public Transform tileParent;

    [Header("Blocking")]
    public LayerMask blockerMask;

    void Start()
    {
        SpawnTiles();
    }


    public void SpawnTiles()
    {
        if (tilePrefab == null)
        {
            Debug.LogWarning("No tile prefab assigned!");
            return;
        }

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 position = CellToWorld(cell);

                if (drawGrid)
                {
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, tileParent);

                    tile.name = $"Tile_{x}_{y}";

                    SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();

                    if (renderer != null)
                    {
                        renderer.color = (x + y) % 2 == 0
                            ? color1
                            : color2;
                    }
                }

            }
        }
    }


    public Vector2Int WorldToCell(Vector2 w) => new Vector2Int(Mathf.RoundToInt((w.x - origin.x) / cellSize), Mathf.RoundToInt((w.y - origin.y) / cellSize));

    public Vector3 CellToWorld(Vector2Int c, float z = 0f) =>
        new Vector3(
            origin.x + c.x * cellSize,
            origin.y + c.y * cellSize,
            z
        );

    public bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.y >= 0 &&
        c.x < size.x && c.y < size.y;

    public bool IsBlocked(Vector2Int c) =>
        !InBounds(c) ||
        Physics2D.OverlapPoint(CellToWorld(c), blockerMask) != null;
}
