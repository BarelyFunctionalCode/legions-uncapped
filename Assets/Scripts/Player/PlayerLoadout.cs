using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLoadout : MonoBehaviour
{
    [SerializeField] private List<GameObject> weaponPrefabObjList;
    [SerializeField] private GameObject throwablePrefabObj;
    private List<GameObject> currentWeaponsObjList;
    private int currentWeaponIndex = 0;
    private GameObject equipedWeapon;

    public void Initialize(PlayerController playerController, Transform weaponMountPoint, Transform throwableMountPoint)
    {
        currentWeaponsObjList = new List<GameObject>();
        foreach (GameObject weaponPrefabObj in weaponPrefabObjList)
        {
            AddWeapon(weaponPrefabObj, playerController, weaponMountPoint);
        }

        AddThrowable(throwablePrefabObj, throwableMountPoint);
    }


    private void AddWeapon(GameObject weaponPrefabObj, PlayerController playerController, Transform weaponMountPoint)
    {
        GameObject newWeapon = GameObject.Instantiate(
            weaponPrefabObj,
            weaponMountPoint.position,
            weaponMountPoint.rotation,
            weaponMountPoint
        );
        newWeapon.GetComponent<Weapon>().Initialize(playerController);

        currentWeaponsObjList.Add(newWeapon);

        if (currentWeaponsObjList.Count - 1 != currentWeaponIndex) newWeapon.GetComponent<Weapon>().Unequip();
        else newWeapon.GetComponent<Weapon>().Equip();
    }

    private void AddThrowable(GameObject throwablePrefabObj, Transform throwableMountPoint)
    {
        GameObject newThrowable = GameObject.Instantiate(
            throwablePrefabObj,
            throwableMountPoint.position,
            throwableMountPoint.rotation,
            throwableMountPoint
        );
        newThrowable.GetComponent<ThrowableManager>().Initialize(transform);
    }


    public void NextWeapon(InputAction.CallbackContext _)
    {
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().Unequip();
        currentWeaponIndex = (currentWeaponIndex + 1) % currentWeaponsObjList.Count;
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().Equip();
    }

    public void PreviousWeapon(InputAction.CallbackContext _)
    {
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().Unequip();
        currentWeaponIndex = (currentWeaponIndex - 1 + currentWeaponsObjList.Count) % currentWeaponsObjList.Count;
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().Equip();
    }
}
