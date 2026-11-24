using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Attack Knockback")]
public class AttackKnockbackUpgrade : Upgrade
{
    public int baseDistance = 1;
    public int distancePerStack = 1;

    public override void Apply(PlayerController p, int stack)
    {
        p.attackKnockback = baseDistance + (stack - 1) * distancePerStack;
    }
}