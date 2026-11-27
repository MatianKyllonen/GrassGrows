using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Attack Fire Tile")]
public class AttackFireTileUpgrade : ScriptableObject
{
    public GameObject firePrefab;

    public float baseDuration = 2f;
    public float durationPerStack = 1f;

    public int baseDamage = 1;
    public int damagePerStack = 3;

    public void Apply(PlayerController player, int stack)
    {
        AttackFireTileEffect effect =
            player.GetComponent<AttackFireTileEffect>();

        if (effect == null)
            effect = player.gameObject.AddComponent<AttackFireTileEffect>();

        float duration = baseDuration + (stack - 1) * durationPerStack;
        int damage = baseDamage + (stack - 1) * damagePerStack;

        effect.Configure(firePrefab, duration, damage);
    }
}