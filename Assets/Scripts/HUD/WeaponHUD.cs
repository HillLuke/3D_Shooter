using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeaponHUD : MonoBehaviour
{
    public TMP_Text Name;
    public TMP_Text Ammo;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
           WeaponController playerWeapon = PlayerController.Instance?.PlayerWeaponsManager.GetActiveWeapon();
        Name.text = playerWeapon.weaponName;
        Ammo.text = $"{playerWeapon.currentAmmo}/{playerWeapon.maxAmmo}";
    }
}
