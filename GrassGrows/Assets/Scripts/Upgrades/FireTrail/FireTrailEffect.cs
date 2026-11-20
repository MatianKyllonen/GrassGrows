using UnityEngine;

public class FireTrailEffect : MonoBehaviour
{
    private PlayerController player;
    private FireTile prefab;
    private float duration;
    private int damage;

    private void Awake() => player = GetComponent<PlayerController>();
    private void OnEnable() => player.onDashed += OnDashed;
    private void OnDisable() => player.onDashed -= OnDashed;

    public void Configure(FireTile prefab, float duration, int damage)
    {
        this.prefab = prefab;
        this.duration = duration;
        this.damage = damage;
    }

    // Light every square the dash passes through
    private void OnDashed(Vector2Int from, Vector2Int to)
    {
        if (!prefab) return;
        var step = new Vector2Int(System.Math.Sign(to.x - from.x), System.Math.Sign(to.y - from.y));
        if (step == Vector2Int.zero) return;

        for (var c = from; ; c += step)
        {
            var pos = player.board.CellToWorld(c, prefab.transform.position.z);
            Instantiate(prefab, pos, Quaternion.identity)
                .Init(duration, damage, player.enemyMask, player.board.cellSize);
            if (c == to) break;
        }
    }
}