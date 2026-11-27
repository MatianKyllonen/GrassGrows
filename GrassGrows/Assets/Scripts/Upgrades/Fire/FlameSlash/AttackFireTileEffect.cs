using UnityEngine;

public class AttackFireTileEffect : MonoBehaviour
{
    private PlayerController player;
    private BoardGrid board;

    private GameObject firePrefab;
    private float fireDuration;
    private int fireDamage;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (player != null)
            player.onAttackCellHit += OnAttackCellHit;
    }

    private void OnDisable()
    {
        if (player != null)
            player.onAttackCellHit -= OnAttackCellHit;
    }

    public void Configure(GameObject prefab, float duration, int damage)
    {
        firePrefab = prefab;
        fireDuration = duration;
        fireDamage = damage;

        if (GameManager.instance != null)
            board = GameManager.instance.board;
    }

    private void OnAttackCellHit(Vector2Int cell)
    {
        if (firePrefab == null || player == null || board == null)
            return;

        if (FireTile.TryGetAt(cell, out FireTile existingTile))
        {
            existingTile.Init(
                fireDuration,
                fireDamage,
                player.enemyMask,
                board.cellSize,
                cell
            );

            return;
        }

        Vector3 position = board.CellToWorld(
            cell,
            firePrefab.transform.position.z
        );

        GameObject tileObject = Instantiate(firePrefab, position, Quaternion.identity);

        FireTile tile = tileObject.GetComponent<FireTile>();

        if (tile != null)
        {
            tile.Init(
                fireDuration,
                fireDamage,
                player.enemyMask,
                board.cellSize,
                cell
            );
        }
    }
}