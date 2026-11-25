using UnityEngine;

public static class StatApplier
{
    // Convention for every stat: a POSITIVE amount is good for the player, a negative one is bad.
    // Time-based stats are percentages (0.15 = 15% faster, -0.15 = 15% slower).
    public static void Apply(PlayerController p, Stat stat, float amount)
    {
        float cut = 1f - amount;
        int flat = Mathf.RoundToInt(amount);

        switch (stat)
        {
            case Stat.MaxHealth: p.AddMaxHealth(flat); break;
            case Stat.DashDamage: p.dashDamage = Mathf.Max(1, p.dashDamage + flat); break;
            case Stat.AttackDamage: p.attackDamage = Mathf.Max(1, p.attackDamage + flat); break;
            case Stat.DashDistance: p.dashDistance = Mathf.Max(1, p.dashDistance + flat); break;
            case Stat.Invincibility: p.invincibilityTime = Mathf.Max(0.2f, p.invincibilityTime + amount); break;

            case Stat.DashChargeTime:
                p.dashChargeTime = Mathf.Clamp(p.dashChargeTime * cut, 0.3f, 3f);
                p.RefreshChargeSpeed();
                break;
            case Stat.DashCooldown:
                p.dashCooldown = Mathf.Clamp(p.dashCooldown * cut, 0f, 3f);
                break;
            case Stat.StepSpeed:
                p.stepTime = Mathf.Max(0.06f, p.stepTime * cut);
                p.RefreshHopSpeed();
                break;
            case Stat.AttackSpeed:
                p.attackStartup = Mathf.Clamp(p.attackStartup * cut, 0.03f, 0.5f);
                p.attackRecovery = Mathf.Clamp(p.attackRecovery * cut, 0.05f, 0.8f);
                break;
        }
    }

    public static bool IsPercent(Stat s) =>
        s == Stat.DashChargeTime || s == Stat.DashCooldown ||
        s == Stat.AttackSpeed || s == Stat.StepSpeed;

    // Worded so that + is always good: "Dash Charge Speed", not "Dash Charge Time"
    public static string Label(Stat s)
    {
        switch (s)
        {
            case Stat.MaxHealth: return "Max Health";
            case Stat.DashDamage: return "Dash Damage";
            case Stat.AttackDamage: return "Attack Damage";
            case Stat.DashDistance: return "Dash Distance";
            case Stat.Invincibility: return "Invincibility Time";
            case Stat.DashChargeTime: return "Dash Charge Speed";
            case Stat.DashCooldown: return "Dash Recovery Speed";
            case Stat.StepSpeed: return "Movement Speed";
            case Stat.AttackSpeed: return "Attack Speed";
            default: return s.ToString();
        }
    }
}