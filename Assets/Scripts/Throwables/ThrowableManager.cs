using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowableManager : MonoBehaviour
{
    public static List<string> interactionTags = new List<string>() { "Terrain", "Player", "Projectile" };

    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] private GameObject throwablePrefabObj;
    [SerializeField] private ThrowableUI throwableUI;

    private float ammoCount = 50;
    private float maxAmmo = 50;

    private float fireRate = 2;
    private float fireRateTimer = 2;

    private Transform ownerTransform;
    private PlayerControls playerControls;
    protected Camera playerCamera;

    private bool canThrow = true;
    private bool isThrowing = false;
    private bool startedThrow = false;

    private float holdThrowDebounce = 0.05f;
    private float holdThrowDebounceTimer = 0f;

    private float throwForceFactor = 0f;
    private float throwForceFactorIncreaseRate = 0.01f;


    private void Awake()
    {
        playerCamera = Camera.main;
    }

    public void Initialize(Transform ownerTransform)
    {
        this.ownerTransform = ownerTransform;
        playerControls = ownerTransform.GetComponent<PlayerController>().playerControls;

        playerControls.Equipment.Throwable.started += OnThrowableStarted;
        playerControls.Equipment.Throwable.canceled += OnThrowableCanceled;

        throwableUI.Initialize(maxAmmo);
    }

    private void OnDisable()
    {
        playerControls.Equipment.Throwable.started -= OnThrowableStarted;
        playerControls.Equipment.Throwable.canceled -= OnThrowableCanceled;
    }

    private void OnThrowableStarted(InputAction.CallbackContext _)
    {
        isThrowing = true;
    }

    private void OnThrowableCanceled(InputAction.CallbackContext _)
    {
        isThrowing = false;
    }

    private void Update()
    {
        Vector3 newWeaponAimPosition = playerCamera.transform.position + playerCamera.transform.forward * 1000f;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, ~ignoreLayers))
            newWeaponAimPosition = hitInfo.point;

        transform.LookAt(newWeaponAimPosition);

        if (isThrowing) StartThrow();
        if (!isThrowing) ReleaseThrow();

        if (!canThrow)
        {
            fireRateTimer += Time.deltaTime;

            if (ammoCount > 0 && fireRateTimer >= fireRate)
            {
                fireRateTimer = 0;
                canThrow = true;
            }
        }

        throwableUI.UpdateUI(ammoCount, throwForceFactor);
    }

    private void StartThrow()
    {
        if (!canThrow) return;

        holdThrowDebounceTimer += Time.fixedDeltaTime;

        if (holdThrowDebounceTimer >= holdThrowDebounce)
        {
            throwForceFactor += Mathf.Clamp01(throwForceFactorIncreaseRate);
        }

        startedThrow = true;
    }

    private void ReleaseThrow()
    {
        if (!canThrow || !startedThrow) return;

        GameObject throwableObj = Instantiate(throwablePrefabObj, transform.position + transform.forward, transform.rotation);
        throwableObj.GetComponent<Throwable>().Throw(ownerTransform, throwForceFactor);
        ammoCount--;
        canThrow = false;
        startedThrow = false;
        throwForceFactor = 0;
        holdThrowDebounceTimer = 0;
    }

    public void refillAmmo()
    {
        ammoCount = maxAmmo;
    }
}
