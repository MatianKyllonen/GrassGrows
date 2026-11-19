using UnityEngine;
using UnityEngine.Events;

public class PlayerBase : MonoBehaviour
{
    public static PlayerBase Instance { get; private set; }

    public int maxHealth = 20;
    public UnityEvent<int, int> onHealthChanged;   // current, max (hook up a health bar)
    public UnityEvent onDamaged;                   // shake / flash / sound
    public UnityEvent onDestroyed;                 // game over screen

    public int Health { get; private set; }
    public bool IsDestroyed => Health <= 0;

    private void Awake()
    {
        Instance = this;
        Health = maxHealth;
    }

    private void Start() => onHealthChanged?.Invoke(Health, maxHealth);

    public void TakeDamage(int amount)
    {
        if (IsDestroyed) return;

        Health = Mathf.Max(0, Health - amount);
        onDamaged?.Invoke();
        onHealthChanged?.Invoke(Health, maxHealth);

        if (IsDestroyed)
        {
            Debug.Log("Base destroyed!");
            onDestroyed?.Invoke();
        }
    }
}