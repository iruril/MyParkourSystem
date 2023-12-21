using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShootingMode
{
    Single = 0,
    Burst = 1,
    Auto = 2
}

[System.Serializable]
public class WeaponStat
{
    public float AimSpeed;
    public float RoundDamage;
    public int MagazineCapacity;
    public int RoundsPerMinute;
    public int MuzzleVelocity;
    public List<ShootingMode> ShootingModes = new List<ShootingMode> { ShootingMode.Single};

    private float _fireRate;
    public WaitForSeconds FireRateWFS { get; private set; } = null;

    public WeaponStat(float AimSpeed, float RoundDamage, int MagazineCapacity, int RoundsPerMinute, int MuzzleVelocity, List<ShootingMode> ShootingModes)
    {
        this.AimSpeed = AimSpeed;
        this.RoundDamage = RoundDamage;
        this.MagazineCapacity = MagazineCapacity;
        this.RoundsPerMinute = RoundsPerMinute;
        this.MuzzleVelocity = MuzzleVelocity;
        if (ShootingModes.Count > 0)
        {
            this.ShootingModes = ShootingModes;
        }

        _fireRate = 1 / (float)(this.RoundsPerMinute / 60);
        FireRateWFS = YieldCache.WaitForSeconds(_fireRate);
    }
}

[System.Serializable]
public class Weapon
{
    public string WeaponID;
    public string WeaponName;
    public WeaponStat WeaponStat;

    public Weapon(string WeaponID, string WeaponName, WeaponStat WeaponStat)
    {
        this.WeaponID = WeaponID;
        this.WeaponName = WeaponName;
        this.WeaponStat = WeaponStat;
    }
}
