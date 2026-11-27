using UnityEngine;

public class AttackProjectileEffect : MonoBehaviour
{
    private PlayerController player;
    private Projectile prefab;
    private int range, damage, pierce;

    private void Awake() => player = GetComponent<PlayerController>();
    private void OnEnable() { if (player) player.onAttackCellHit += OnAttack; }
    private void OnDisable() { if (player) player.onAttackCellHit -= OnAttack; }

    public void Configure(Projectile prefab, int range, int damage, int pierce)
    {
        this.prefab = prefab;
        this.range = range;
        this.damage = damage;
        this.pierce = pierce;
    }

    private void OnAttack(Vector2Int target)
    {
        if (!prefab || !player) return;

        Vector2Int dir = target - player.Cell;   // the swing direction
        if (dir == Vector2Int.zero) return;

        var p = Instantiate(prefab, player.board.CellToWorld(player.Cell, prefab.transform.position.z), Quaternion.identity);
        p.Init(player.board, player.enemyMask, player.Cell, dir, range, damage, pierce);
    }
}