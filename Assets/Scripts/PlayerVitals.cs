using System;
using UnityEngine;

public enum PlayerResourceType
{
    Health,
    Magic,
    Stamina
}

/// <summary>
/// Owns the player's three core resources. Gameplay systems write through this
/// component, while UI and other systems can subscribe without knowing how the
/// values are stored.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerVitals : MonoBehaviour
{
    [Header("Maximum values")]
    [SerializeField, Min(0f)] private float maximumHealth = 100f;
    [SerializeField, Min(0f)] private float maximumMagic = 100f;
    [SerializeField, Min(0f)] private float maximumStamina = 100f;

    [Header("Starting values")]
    [SerializeField, Min(0f)] private float startingHealth = 100f;
    [SerializeField, Min(0f)] private float startingMagic = 100f;
    [SerializeField, Min(0f)] private float startingStamina = 100f;

    [Header("Regeneration")]
    [SerializeField, Min(0f)] private float staminaRegenerationPerSecond = 10f;

    private float currentHealth;
    private float currentMagic;
    private float currentStamina;
    private float staminaRegenerationDelayRemaining;
    private bool deathRaised;

    public event Action<PlayerResourceType, float, float> ResourceChanged;
    public event Action<float, Vector3, Vector3> Damaged;
    public event Action Died;

    public float CurrentHealth => currentHealth;
    public float CurrentMagic => currentMagic;
    public float CurrentStamina => currentStamina;
    public float MaximumHealth => maximumHealth;
    public float MaximumMagic => maximumMagic;
    public float MaximumStamina => maximumStamina;
    public float StaminaRegenerationPerSecond => staminaRegenerationPerSecond;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        ResetToStartingValues();
    }

    private void Update()
    {
        staminaRegenerationDelayRemaining = Mathf.Max(
            0f,
            staminaRegenerationDelayRemaining - Time.deltaTime);

        if (staminaRegenerationDelayRemaining > 0f)
        {
            return;
        }

        if (currentStamina < maximumStamina && staminaRegenerationPerSecond > 0f)
        {
            Modify(PlayerResourceType.Stamina, staminaRegenerationPerSecond * Time.deltaTime);
        }
    }

    public void DelayStaminaRegeneration(float delaySeconds)
    {
        staminaRegenerationDelayRemaining = Mathf.Max(
            staminaRegenerationDelayRemaining,
            Mathf.Max(0f, delaySeconds));
    }

    public float GetCurrent(PlayerResourceType resourceType)
    {
        return resourceType switch
        {
            PlayerResourceType.Health => CurrentHealth,
            PlayerResourceType.Magic => CurrentMagic,
            PlayerResourceType.Stamina => CurrentStamina,
            _ => 0f
        };
    }

    public float GetMaximum(PlayerResourceType resourceType)
    {
        return resourceType switch
        {
            PlayerResourceType.Health => MaximumHealth,
            PlayerResourceType.Magic => MaximumMagic,
            PlayerResourceType.Stamina => MaximumStamina,
            _ => 0f
        };
    }

    public void SetCurrent(PlayerResourceType resourceType, float value)
    {
        float maximum = GetMaximum(resourceType);
        float clampedValue = Mathf.Clamp(value, 0f, maximum);

        switch (resourceType)
        {
            case PlayerResourceType.Health:
                SetValue(ref currentHealth, clampedValue, resourceType);
                break;
            case PlayerResourceType.Magic:
                SetValue(ref currentMagic, clampedValue, resourceType);
                break;
            case PlayerResourceType.Stamina:
                SetValue(ref currentStamina, clampedValue, resourceType);
                break;
        }
    }

    public void Modify(PlayerResourceType resourceType, float delta)
    {
        SetCurrent(resourceType, GetCurrent(resourceType) + delta);
    }

    public bool ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return false;
        }

        float previousHealth = currentHealth;
        SetCurrent(PlayerResourceType.Health, currentHealth - amount);
        float appliedDamage = previousHealth - currentHealth;
        if (appliedDamage <= 0f)
        {
            return false;
        }

        Damaged?.Invoke(appliedDamage, hitPoint, hitDirection);
        if (IsDead && !deathRaised)
        {
            deathRaised = true;
            Died?.Invoke();
        }

        return true;
    }

    public bool TrySpend(PlayerResourceType resourceType, float amount)
    {
        if (amount < 0f || GetCurrent(resourceType) < amount)
        {
            return false;
        }

        Modify(resourceType, -amount);
        return true;
    }

    public void ResetToStartingValues()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0f, maximumHealth);
        currentMagic = Mathf.Clamp(startingMagic, 0f, maximumMagic);
        currentStamina = Mathf.Clamp(startingStamina, 0f, maximumStamina);
        deathRaised = IsDead;

        ResourceChanged?.Invoke(PlayerResourceType.Health, currentHealth, maximumHealth);
        ResourceChanged?.Invoke(PlayerResourceType.Magic, currentMagic, maximumMagic);
        ResourceChanged?.Invoke(PlayerResourceType.Stamina, currentStamina, maximumStamina);
    }

    private void SetValue(ref float field, float value, PlayerResourceType resourceType)
    {
        if (Mathf.Approximately(field, value))
        {
            return;
        }

        field = value;
        ResourceChanged?.Invoke(resourceType, value, GetMaximum(resourceType));
    }
}
