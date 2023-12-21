using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackStatus
{
    public string CurrentWeapon = string.Empty;
    public int CurrentRound;

    public float AimSpeed;
    public float RoundDamage;
    public int MagazineCapacity;
    public int RoundsPerMinute;
    public int MuzzleVelocity;
    public List<ShootingMode> ShootingModes = new List<ShootingMode> { ShootingMode.Single };

    public WaitForSeconds FireRateWFS { get; private set; } = null;

    private Dictionary<string, int> _weaponHistory = new();

    public void SwapStat(Weapon weapon)
    {
        if (CurrentWeapon == weapon.WeaponName) return;
        _weaponHistory[CurrentWeapon] = CurrentRound;

        CurrentWeapon = weapon.WeaponName;

        AimSpeed = weapon.WeaponStat.AimSpeed;
        RoundDamage = weapon.WeaponStat.RoundDamage;
        MagazineCapacity = weapon.WeaponStat.MagazineCapacity;
        RoundsPerMinute = weapon.WeaponStat.RoundsPerMinute;

        ShootingModes = weapon.WeaponStat.ShootingModes;
        FireRateWFS = weapon.WeaponStat.FireRateWFS;

        if (_weaponHistory.ContainsKey(CurrentWeapon))
        {
            CurrentRound = _weaponHistory[CurrentWeapon];
        }
        else
        {
            CurrentRound = MagazineCapacity;
        }
    }
}
