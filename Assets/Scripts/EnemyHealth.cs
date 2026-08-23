using System;
using UnityEngine;

/// <summary>
/// Owns the health and death state for an enemy. Combat sources only need to
/// call <see cref="TakeDamage"/>, keeping enemy behavior separate from damage.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maximumHealth = 30f;
    [SerializeField, Min(0f)] private float startingHealth = 30f;
    [SerializeField, Min(0f)] private float destroyDelay = 2f;

    private float currentHealth;

    public event Action<EnemyHealth> Died;

    public float CurrentHealth => currentHealth;
    public float MaximumHealth => maximumHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0f, maximumHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public bool TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return false;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f)
        {
            Die();
        }

        return true;
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        Died?.Invoke(this);

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        if (destroyDelay > 0f)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
