using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityHitBounds : MonoBehaviour
{
    public IDamageable MyDamagableEntity = null;
    private bool _isInitialized = false;

    private void Start()
    {
        this.transform.gameObject.layer = LayerMask.NameToLayer(Constants.EntityHitLayer);
        this.transform.tag = Constants.EntityHitBound;
        _isInitialized = true;
    }

    public void OnDamaged(float dealtDamage, Vector3 attackDirection)
    {
        if (!_isInitialized) return;
        MyDamagableEntity.TakeHit(dealtDamage, attackDirection);
    }
}
