using UnityEngine;

public interface IDamageable
{
    void TakeHit(float damage, Vector3 damagedDir);
}
