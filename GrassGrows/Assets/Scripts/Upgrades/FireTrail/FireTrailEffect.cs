using UnityEngine;

public class FireTrailEffect : MonoBehaviour
{
    private PlayerController player;
    private FireTile prefab;

    private float duration;
    private int damage;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (player)
            player.onDashCellPassed += OnCellPassed;
    }

    private void OnDisable()
    {
        if (player)
            player.onDashCellPassed -= OnCellPassed;
    }

    public void Configure(FireTile prefab, float duration, int damage)
    {
        this.prefab = prefab;
        this.duration = duration;
        this.damage = damage;
    }

    // Called once for each square, only after the player has dashed through it
    private void OnCellPassed(Vector2Int c)
    {
        if (!prefab || !player)
            return;

        if (FireTile.TryGetAt(c, out FireTile existing))
        {
            // Refresh the existing tile instead of spawning another
            existing.Refresh(
                duration,
                damage,
                player.enemyMask,
                player.board.cellSize
            );
            return;
        }

        Vector3 pos = player.board.CellToWorld(
            c,
            prefab.transform.position.z
        );

        FireTile newTile = Instantiate(prefab, pos, Quaternion.identity);

        newTile.Init(
            duration,
            damage,
            player.enemyMask,
            player.board.cellSize,
            c
        );
    }
}