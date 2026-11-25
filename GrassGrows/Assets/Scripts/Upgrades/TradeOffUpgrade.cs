using System.Text;
using UnityEngine;

[System.Serializable]
public class StatChange
{
    public Stat stat;
    [Tooltip("Positive = good for the player, negative = bad. Time stats are fractions (0.2 = 20%).")]
    public float amount = 0.2f;
}

[CreateAssetMenu(menuName = "Upgrades/Tradeoff Upgrade")]
public class TradeoffUpgrade : Upgrade
{
    public StatChange[] changes;

    [Tooltip("Builds the card text from the changes, green for buffs and red for drawbacks. Turn off to use the Description field instead.")]
    public bool autoDescription = true;

    public override void Apply(PlayerController p, int stack)
    {
        foreach (var c in changes)
            StatApplier.Apply(p, c.stat, c.amount);
    }

    public override string GetDescription()
    {
        if (!autoDescription) return description;

        var sb = new StringBuilder();
        foreach (var c in changes)
        {
            string color = c.amount >= 0f ? "#6fdc6f" : "#ff6b6b";
            string sign = c.amount >= 0f ? "+" : "-";
            float abs = Mathf.Abs(c.amount);
            string value = StatApplier.IsPercent(c.stat)
                ? Mathf.RoundToInt(abs * 100f) + "%"
                : abs.ToString("0.##");

            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"<color={color}>{sign}{value} {StatApplier.Label(c.stat)}</color>");
        }
        if (!string.IsNullOrEmpty(description)) sb.Append('\n').Append(description);
        return sb.ToString();
    }
}