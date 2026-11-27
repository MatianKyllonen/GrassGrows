using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Attack Projectile")]
public class ProjectileUpgrade : Upgrade
{
    public Projectile projectilePrefab;
    public int baseRange = 3;
    public int rangePerStack = 1;
    public int baseDamage = 1;
    [Tooltip("+1 damage every this many stacks")]
    public int damageEvery = 2;
    [Tooltip("+1 pierce every this many stacks (0 = never)")]
    public int pierceEvery = 0;

    public override void Apply(PlayerController p, int stack)
    {
        var fx = p.GetComponent<AttackProjectileEffect>();
        if (!fx) fx = p.gameObject.AddComponent<AttackProjectileEffect>();

        int range = baseRange + (stack - 1) * rangePerStack;
        int damage = baseDamage + (damageEvery > 0 ? (stack - 1) / damageEvery : 0);
        int pierce = pierceEvery > 0 ? (stack - 1) / pierceEvery : 0;
        fx.Configure(projectilePrefab, range, damage, pierce);
    }
}