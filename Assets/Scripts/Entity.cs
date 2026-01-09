using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    private float health = 0;
    private float maxHealth = 100;

    private float energy = 0;
    private float maxEnergy = 33;
    private float energyRegenRate = 6.875f;
    private float groundEnergyRegenRate = 12.5f;

    private bool isDead = false;
    protected bool isGrounded = false;

    protected virtual void Awake()
    {
        health = maxHealth;
        energy = maxEnergy;
    }

    protected virtual void Update()
    {
        if (isDead) return;

        ApplyEnergyDelta((isGrounded ? groundEnergyRegenRate : energyRegenRate) * Time.deltaTime);
    }

    public float GetHealth() { return health; }
    public float GetHealthPercentage() { return health / maxHealth; }
    public void TakeDamage(float damage)
    {
        ApplyhealthDelta(-damage);
        if (health <= 0) Die();
    }
    public void ApplyhealthDelta(float amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    public float GetEnergy() { return energy; }
    public float GetEnergyPercentage() { return energy / maxEnergy; }
    public void ApplyEnergyDelta(float amount)
    {
        energy += amount;
        energy = Mathf.Clamp(energy, 0, maxEnergy);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDie();
        Destroy(gameObject);
    }

    protected virtual void OnDie() {}
}
