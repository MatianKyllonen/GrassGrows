using UnityEngine;

public enum Stat
{
    MaxHealth, DashDamage, AttackDamage, DashDistance,
    DashChargeTime, DashCooldown, AttackSpeed, StepSpeed, Invincibility, HealHoldTime, HealAmount
}

[CreateAssetMenu(menuName = "Upgrades/Stat Upgrade")]
public class StatUpgrade : Upgrade
{
    public Stat stat;
    [Tooltip("Flat amount for damage/health/distance/invincibility. A fraction (0.15 = 15%) for the time-based stats.")]
    public float amount = 1f;

    public override void Apply(PlayerController p, int stack) => StatApplier.Apply(p, stat, amount);
}