using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Heal Shockwave")]
public class HealShockwaveUpgrade : Upgrade
{
    public int baseRadius = 2;
    public int radiusPerStack = 1;
    public int baseDistance = 1;
    public int distancePerStack = 1;

    public override void Apply(PlayerController p, int stack)
    {
        p.healKnockbackRadius = baseRadius + (stack - 1) * radiusPerStack;
        p.healKnockbackDistance = baseDistance + (stack - 1) * distancePerStack;
    }
}