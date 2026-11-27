
using System.Collections.Generic;
using UnityEngine;

public class FireTile : MonoBehaviour
{
    public float tickInterval = 0.4f;
    public float fadeTime = 0.5f;

    private static Dictionary<Vector2Int, FireTile> activeTiles =
        new Dictionary<Vector2Int, FireTile>();

    private float lifetime;
    private float age;
    private float tick;
    private float cellSize;

    private int damage;
    private LayerMask mask;

    private Vector2Int cell;
    private bool registered;

    private SpriteRenderer sr;

    public void Init(
        float lifetime,
        int damage,
        LayerMask mask,
        float cellSize,
        Vector2Int cell)
    {
        this.cell = cell;

        Refresh(lifetime, damage, mask, cellSize);

        sr = GetComponentInChildren<SpriteRenderer>();

        Register();
    }

    public void Refresh(
        float lifetime,
        int damage,
        LayerMask mask,
        float cellSize)
    {
        // Reset the existing tile instead of creating another.
        this.lifetime = lifetime;
        this.damage = damage;
        this.mask = mask;
        this.cellSize = cellSize;

        age = 0f;
        tick = 0f;

        if (sr)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    private void Register()
    {
        if (activeTiles.TryGetValue(cell, out FireTile existing))
        {
            if (existing != null && existing != this)
                activeTiles.Remove(cell);
        }

        activeTiles[cell] = this;
        registered = true;
    }

    public static bool TryGetAt(Vector2Int cell, out FireTile tile)
    {
        if (activeTiles.TryGetValue(cell, out tile))
        {
            if (tile != null)
                return true;

            activeTiles.Remove(cell);
        }

        tile = null;
        return false;
    }

    private void Update()
    {
        age += Time.deltaTime;
        tick -= Time.deltaTime;

        if (tick <= 0f)
        {
            tick = tickInterval;

            var hits = Physics2D.OverlapBoxAll(
                transform.position,
                Vector2.one * cellSize * 0.8f,
                0f,
                mask
            );

            foreach (var h in hits)
            {
                if (h.TryGetComponent<IDamageable>(out var d))
                    d.Hit(transform.position, damage);
            }
        }

        if (sr)
        {
            Color c = sr.color;
            c.a = fadeTime > 0f
                ? Mathf.Clamp01((lifetime - age) / fadeTime)
                : 1f;

            sr.color = c;
        }

        if (age >= lifetime)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (registered &&
            activeTiles.TryGetValue(cell, out FireTile existing) &&
            existing == this)
        {
            activeTiles.Remove(cell);
        }
    }
}
