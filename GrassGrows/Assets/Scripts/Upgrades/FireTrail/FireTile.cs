using UnityEngine;

public class FireTile : MonoBehaviour
{
    public float tickInterval = 0.4f;
    public float fadeTime = 0.5f;

    private float lifetime, age, tick, cellSize;
    private int damage;
    private LayerMask mask;
    private SpriteRenderer sr;

    public void Init(float lifetime, int damage, LayerMask mask, float cellSize)
    {
        this.lifetime = lifetime;
        this.damage = damage;
        this.mask = mask;
        this.cellSize = cellSize;
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        age += Time.deltaTime;
        tick -= Time.deltaTime;

        if (tick <= 0f)
        {
            tick = tickInterval;
            var hits = Physics2D.OverlapBoxAll(transform.position, Vector2.one * cellSize * 0.8f, 0f, mask);
            foreach (var h in hits)
                if (h.TryGetComponent<IDamageable>(out var d)) d.Hit(transform.position, damage);
        }

        if (sr)
        {
            var c = sr.color;
            c.a = Mathf.Clamp01((lifetime - age) / fadeTime);   // fade out at the end
            sr.color = c;
        }

        if (age >= lifetime) Destroy(gameObject);
    }
}