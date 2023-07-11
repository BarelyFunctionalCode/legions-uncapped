using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThrowableUI : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private TMP_Text maxAmmoText;
    [SerializeField] private Image fireRateSlider;

    public void Initialize(float maxAmmo)
    {
        maxAmmoText.text = maxAmmo.ToString();
        ammoCountText.text = maxAmmo.ToString();
    }

    public void UpdateUI(float ammoCount, float throwForceFactor)
    {
        ammoCountText.text = ammoCount.ToString();
        fireRateSlider.fillAmount = throwForceFactor;
        if (throwForceFactor > 0) fireRateSlider.enabled = true;
        else fireRateSlider.enabled = false;
    }
}
