using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public UnityAction<float, GameObject> onDamaged;
    public UnityAction<float> onHealed;
    public UnityAction onDie;

    public float MaxHealth = 100;
    public float CurrentHealth;
    public bool Invincible;
    public bool IsAlive;

    private void Start()
    {
        CurrentHealth = MaxHealth;
        Invincible = false;
        IsAlive = true;
    }

    public void Heal(float healAmount)
    {
        float healthBefore = CurrentHealth;
        CurrentHealth += healAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        // call OnHeal action
        float trueHealAmount = CurrentHealth - healthBefore;
        if (trueHealAmount > 0f && onHealed != null)
        {
            onHealed.Invoke(trueHealAmount);
        }
    }

    public void TakeDamage(float damage, GameObject damageSource)
    {
        if (Invincible)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        // call OnDamage action
        float trueDamageAmount = healthBefore - CurrentHealth;
        if (trueDamageAmount > 0f && onDamaged != null)
        {
            onDamaged.Invoke(trueDamageAmount, damageSource);
        }

        HandleDeath();
    }

    public void Kill()
    {
        CurrentHealth = 0f;

        // call OnDamage action
        if (onDamaged != null)
        {
            onDamaged.Invoke(MaxHealth, null);
        }

        HandleDeath();
    }

    private void HandleDeath()
    {
        if (!IsAlive)
            return;

        // call OnDie action
        if (CurrentHealth <= 0f)
        {
            if (onDie != null)
            {
                IsAlive = false;
                onDie.Invoke();
            }

        }
    }
}
