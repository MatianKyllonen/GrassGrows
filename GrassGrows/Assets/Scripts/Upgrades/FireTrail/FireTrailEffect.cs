
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
            player.onDashed += OnDashed;
    }

    private void OnDisable()
    {
        if (player)
            player.onDashed -= OnDashed;
    }

    public void Configure(FireTile prefab, float duration, int damage)
    {
        this.prefab = prefab;
        this.duration = duration;
        this.damage = damage;
    }

    // Light every square the dash passes through.
    private void OnDashed(Vector2Int from, Vector2Int to)
    {
        if (!prefab || !player)
            return;

        Vector2Int step = new Vector2Int(
            System.Math.Sign(to.x - from.x),
            System.Math.Sign(to.y - from.y)
        );

        if (step == Vector2Int.zero)
            return;

        for (Vector2Int c = from; ; c += step)
        {
            // Check whether this square already contains fire.
            if (FireTile.TryGetAt(c, out FireTile existing))
            {
                // Refresh the existing tile instead of spawning another.
                existing.Refresh(
                    duration,
                    damage,
                    player.enemyMask,
                    player.board.cellSize
                );
            }
            else
            {
                // No fire here, so create a new tile.
                Vector3 pos = player.board.CellToWorld(
                    c,
                    prefab.transform.position.z
                );

                FireTile newTile = Instantiate(
                    prefab,
                    pos,
                    Quaternion.identity
                );

                newTile.Init(
                    duration,
                    damage,
                    player.enemyMask,
                    player.board.cellSize,
                    c
                );
            }

            if (c == to)
                break;
        }
    }
}
