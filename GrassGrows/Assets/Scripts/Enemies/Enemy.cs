using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Unity.Mathematics;

public class Enemy : MonoBehaviour, IDamageable
{
    public int maxHealth = 1;
    public int xpValue = 10;
    public UnityEvent onHit;
    public UnityEvent onDeath;

    private int health;
    private SpriteRenderer spriteRenderer;

    public float hitFlashDuration = 0.1f;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashCoroutine;

    public GameObject boomEffect;

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

        AudioManager.Instance.PlaySound(hurtSFX, 0.5f);

        onHit?.Invoke();

        if (spriteRenderer != null)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(HitFlash());
        }

        if (health <= 0)
            Die();
    }

    private IEnumerator HitFlash()
    {
        float elapsed = 0f;

        while (elapsed < hitFlashDuration)
        {
            float amount = 1f - (elapsed / hitFlashDuration);

            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_FlashAmount", amount);
            spriteRenderer.SetPropertyBlock(propertyBlock);

            elapsed += Time.deltaTime;
            yield return null;
        }

        propertyBlock.SetFloat("_FlashAmount", 0f);
        spriteRenderer.SetPropertyBlock(propertyBlock);

        flashCoroutine = null;
    }

    private void Die()
    {
        GameObject boom = Instantiate(boomEffect, transform.position, quaternion.identity);
        if (PlayerProgression.Instance) PlayerProgression.Instance.AddXP(xpValue);
        Destroy(boom, 2);
        onDeath?.Invoke();
        Destroy(gameObject);   // swap for a death animation later
    }
}