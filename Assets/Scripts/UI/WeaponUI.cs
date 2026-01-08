using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private TMP_Text maxAmmoText;
    [SerializeField] private Image fireRateSlider;
    [SerializeField] private Image reticleImage;

    public void Initialize(float maxAmmo, Sprite reticleSprite)
    {
        maxAmmoText.text = maxAmmo.ToString();
        ammoCountText.text = maxAmmo.ToString();
        reticleImage.sprite = reticleSprite;
    }

    public void UpdateUI(float ammoCount, float fireRateTimer, float fireRate)
    {
        ammoCountText.text = ammoCount.ToString();
        fireRateSlider.fillAmount = fireRateTimer / fireRate;
        if (fireRateTimer > fireRate || fireRate < 1.0f) fireRateSlider.enabled = false;
        else fireRateSlider.enabled = true;
    }
}
