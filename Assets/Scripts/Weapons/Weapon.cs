using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public static List<string> interactionTags = new List<string>() { "Terrain", "Player", "Throwable" };
    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject modelObj;
    [SerializeField] private GameObject projectilePrefabObj;
    [SerializeField] private Sprite reticleSprite;
    [SerializeField] private WeaponUI weaponUI;

    [SerializeField] private float ammoCount = 10000;
    [SerializeField] private float maxAmmo = 10000;
    [SerializeField] private float damage = 1;

    [SerializeField] private float fireRate = 0.05f;
    private float fireRateTimer = 0;

    [SerializeField] private bool canFire = true;

    private Projectile currentProjectile;
    
    protected PlayerControls playerControls;
    protected Camera playerCamera;

    protected Transform ownerTransform;

    protected bool isFiring = false;

    public bool isEquiped = true;



    private void Awake()
    {
        playerCamera = Camera.main;
    }

    private void OnDisable()
    {
        playerControls.Equipment.PrimaryFire.started -= OnPrimaryFireStarted;
        playerControls.Equipment.PrimaryFire.canceled -= OnPrimaryFireCanceled;

        isFiring = false;
    }

    protected virtual void Update()
    {
        Vector3 newWeaponAimPosition = playerCamera.transform.position + playerCamera.transform.forward * 1000f;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, ~ignoreLayers))
            newWeaponAimPosition = hitInfo.point;

        transform.LookAt(newWeaponAimPosition);

        // Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red);
        // Debug.DrawRay(transform.position, transform.forward * 1000, Color.green);
        // Debug.DrawLine(transform.position, newWeaponAimPosition, Color.blue);

        if (isFiring) Fire();
        if (!isFiring) StopFire();

        if (!canFire)
        {
            fireRateTimer += Time.deltaTime;
            if (ammoCount > 0 && fireRateTimer >= fireRate)
            {
                fireRateTimer = 0;
                canFire = true;
            }
        }

        if (weaponUI.gameObject.activeSelf) weaponUI.UpdateUI(ammoCount, fireRateTimer, fireRate);
    }

    private void OnPrimaryFireStarted(InputAction.CallbackContext _)
    {
        isFiring = true;
    }

    private void OnPrimaryFireCanceled(InputAction.CallbackContext _)
    {
        isFiring = false;
    }

    public void Initialize(PlayerController playerController)
    {
        ownerTransform = playerController.transform;
        playerControls = playerController.playerControls;

        weaponUI.Initialize(maxAmmo, reticleSprite);
    }

    public void Equip()
    {
        modelObj.SetActive(true);
        playerControls.Equipment.PrimaryFire.started += OnPrimaryFireStarted;
        playerControls.Equipment.PrimaryFire.canceled += OnPrimaryFireCanceled;

        weaponUI.gameObject.SetActive(true);
        isEquiped = true;
    }

    public void Unequip()
    {
        modelObj.SetActive(false);
        playerControls.Equipment.PrimaryFire.started -= OnPrimaryFireStarted;
        playerControls.Equipment.PrimaryFire.canceled -= OnPrimaryFireCanceled;

        isFiring = false;

        weaponUI.gameObject.SetActive(false);
        isEquiped = false;
    }

    public void refillAmmo()
    {
        ammoCount = maxAmmo;
    }

    protected virtual void Fire()
    {
        if (!canFire) return;
        if (currentProjectile == null)
        {
            GameObject newProjectileObj = Instantiate(projectilePrefabObj, projectileSpawnPoint.position, projectileSpawnPoint.rotation, projectileSpawnPoint);
            currentProjectile = newProjectileObj.GetComponent<Projectile>();
            currentProjectile.Fire(ownerTransform, damage);
        }
        if (currentProjectile.hasHoldModifier)
        {
            DoHoldModifierStart(currentProjectile);
            return;
        };

        currentProjectile = null;
        ammoCount--;
        canFire = false;
    }

    protected virtual void DoHoldModifierStart(Projectile currentProjectile) { }

    protected virtual void StopFire()
    {
        if (currentProjectile == null || !currentProjectile.hasHoldModifier) return;

        DoHoldModifierEnd(currentProjectile);

        currentProjectile = null;
        ammoCount--;
        canFire = false;
    }

    protected virtual void DoHoldModifierEnd(Projectile currentProjectile) {}
}
