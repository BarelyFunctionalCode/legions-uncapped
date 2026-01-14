using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    private const float energyRegenRate = 6.875f;
    private const float groundEnergyRegenRate = 12.5f;
    
    [Header("Entity Attributes")]
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;

    [SerializeField] private float energy = 0.0f;
    [SerializeField] private float maxEnergy;
    [Range(0.0f, 2.0f)]
    [SerializeField] private float energyRegenFactor = 1.0f;

    private bool isDead = false;
    protected bool isGrounded = false;


    #region Virtual Overrides
    protected virtual void Awake()
    {
        health = maxHealth;
        energy = maxEnergy;
    }

    protected virtual void Update()
    {
        if (isDead) return;
		
        ApplyEnergyDelta((GetIsGrounded() ? groundEnergyRegenRate : energyRegenRate) * energyRegenFactor * Time.deltaTime);
    }
    #endregion
    
    #region Getters
    public float GetHealth()				{ return health; }
    public float GetHealthPercentage()	{ return health / maxHealth; }
    public bool GetIsGrounded() 			{ return isGrounded; }
    #endregion
    

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
