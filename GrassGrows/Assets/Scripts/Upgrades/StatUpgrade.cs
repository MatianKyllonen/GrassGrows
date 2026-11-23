using UnityEngine;

public enum Stat
{
    MaxHealth, DashDamage, AttackDamage, DashDistance,
    DashChargeTime, DashCooldown, AttackSpeed, StepSpeed, Invincibility
}

[CreateAssetMenu(menuName = "Upgrades/Stat Upgrade")]
public class StatUpgrade : Upgrade
{
    public Stat stat;
    [Tooltip("Flat amount for damage/health/distance/invincibility. A fraction (0.15 = 15%) for the time-based stats.")]
    public float amount = 1f;

    public override void Apply(PlayerController p, int stack)
    {
        float cut = 1f - amount;   // for "15% faster" style stats

        switch (stat)
        {
            case Stat.MaxHealth: p.AddMaxHealth((int)amount); break;
            case Stat.DashDamage: p.dashDamage += (int)amount; break;
            case Stat.AttackDamage: p.attackDamage += (int)amount; break;
            case Stat.DashDistance: p.dashDistance += (int)amount; break;
            case Stat.Invincibility:
                p.invincibilityTime += amount;
                break;

            case Stat.DashChargeTime:
                p.dashChargeTime = Mathf.Max(0.3f, p.dashChargeTime * cut);
                p.RefreshChargeSpeed();
                break;
            case Stat.DashCooldown: p.dashCooldown = Mathf.Max(0f, p.dashCooldown * cut); break;

            case Stat.StepSpeed:
                p.stepTime = Mathf.Max(0.06f, p.stepTime * cut);
                p.RefreshHopSpeed();
                break;

            case Stat.AttackSpeed:
                p.attackStartup = Mathf.Max(0.03f, p.attackStartup * cut);
                p.attackRecovery = Mathf.Max(0.05f, p.attackRecovery * cut);
                break;
        }
    }
}