using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private Image energyBar;

    private Entity entity = null;
    private bool isInitialized = false;

    public void Initialize(Entity entity)
    {
        if (isInitialized) return;
        isInitialized = true;

        this.entity = entity;
    }

    private void Update()
    {
        if (!isInitialized) return;

        healthBar.fillAmount = entity.GetHealthPercentage();
        energyBar.fillAmount = entity.GetEnergyPercentage();
    }
}
