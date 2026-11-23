
using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 1;
    public int xpValue = 10;

    public UnityEvent onHit;
    public UnityEvent onDeath;

    private int health;
    private SpriteRenderer spriteRenderer;

    [Header("Hit Flash")]
    public float hitFlashDuration = 0.1f;

    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashCoroutine;

    private static readonly int FlashAmountID =
        Shader.PropertyToID("_FlashAmount");

    [Header("Effects")]
    public GameObject boomEffect;
    public GameObject damageIndicator;

    [Header("Audio")]
    public AudioClip hurtSFX;

    private void Awake()
    {
        health = maxHealth;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        propertyBlock = new MaterialPropertyBlock();
    }

    public void Hit(Vector2 from, int damage)
    {
        if (health <= 0) return;

        health -= damage;

        // Damage indicator
        if (damageIndicator)
        {
            GameObject indicator = Instantiate(
                damageIndicator,
                transform.position,
                Quaternion.identity
            );

            indicator.GetComponent<DamageIndicator>()
                .SetDamageText(damage, "-");
        }

        // Hurt sound
        if (hurtSFX && AudioManager.Instance)
        {
            AudioManager.Instance.PlaySound(hurtSFX, 0.5f);
        }

        onHit?.Invoke();

        // Hit flash
        if (spriteRenderer != null)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(HitFlash());
        }

        if (health <= 0)
            Die();
    }

    // ---------- Hit Flash ----------

    private IEnumerator HitFlash()
    {
        float elapsed = 0f;

        while (elapsed < hitFlashDuration)
        {
            float amount = 1f - (elapsed / hitFlashDuration);

            spriteRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(
                FlashAmountID,
                amount
            );

            spriteRenderer.SetPropertyBlock(propertyBlock);

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Reset flash
        spriteRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(FlashAmountID, 0f);

        spriteRenderer.SetPropertyBlock(propertyBlock);

        flashCoroutine = null;
    }

    // ---------- Death ----------

    private void Die()
    {
        GameObject boom = null;

        if (boomEffect)
        {
            boom = Instantiate(
                boomEffect,
                transform.position,
                Quaternion.identity
            );

            Destroy(boom, 2f);
        }

        if (PlayerProgression.Instance)
            PlayerProgression.Instance.AddXP(xpValue);

        onDeath?.Invoke();

        Destroy(gameObject);
    }
}
