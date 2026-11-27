using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Flame Slash")]
public class FlameSlashUpgrade : Upgrade
{
    public FireTile firePrefab;

    public float baseDuration = 2f;
    public float durationPerStack = 1f;

    public int baseDamage = 1;
    public int damagePerStack = 3;

    public override void Apply(PlayerController p, int stack)
    {
        var fx = p.GetComponent<AttackFireTileEffect>();
        if (!fx)
            fx = p.gameObject.AddComponent<AttackFireTileEffect>();

        float duration = baseDuration + (stack - 1) * durationPerStack;
        int damage = baseDamage + (stack - 1) * damagePerStack;

        fx.Configure(firePrefab.gameObject, duration, damage);
    }
}