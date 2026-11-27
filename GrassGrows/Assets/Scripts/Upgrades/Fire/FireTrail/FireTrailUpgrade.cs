using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Fire Trail")]
public class FireTrailUpgrade : Upgrade
{
    public FireTile firePrefab;
    public float baseDuration = 2f;
    public float durationPerStack = 1f;
    public int baseDamage = 1;

    public override void Apply(PlayerController p, int stack)
    {
        var fx = p.GetComponent<FireTrailEffect>();
        if (!fx) fx = p.gameObject.AddComponent<FireTrailEffect>();

        float duration = baseDuration + (stack - 1) * durationPerStack;
        int damage = baseDamage + (stack - 1) * 3;
        fx.Configure(firePrefab, duration, damage);
    }
}