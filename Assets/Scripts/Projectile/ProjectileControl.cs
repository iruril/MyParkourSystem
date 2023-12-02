using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileControl : MonoBehaviour
{
    public GameObject hitPrefab;
    public Vector3 HitNormal = Vector3.zero;
    public Vector3 HitPoint = Vector3.zero;
    public float AutomaticDestroySec = 10f;

    public float Speed = 100;
    public float Damage = 25;

    void Awake()
    {
        StartCoroutine(LifeTime());
    }

    void Update()
    {
        if (Speed != 0)
        {
            transform.position += transform.forward * (Speed * Time.deltaTime);
        }
    }

    public void Initailize(float damage, Vector3 pos, Vector3 normal)
    {
        Damage = damage;
        HitPoint = pos;
        HitNormal = normal;
    }

    private IEnumerator LifeTime()
    {
        yield return new WaitForSeconds(AutomaticDestroySec);
        Destroy(gameObject);
    } 

    void OnCollisionEnter(Collision co)
    {
        IDamageable damageble = co.transform.GetComponent<IDamageable>();
        if (damageble != null)
        {
            damageble.TakeHit(Damage, this.transform.forward);
        }
        StopCoroutine(LifeTime());

        if (hitPrefab != null && HitNormal != Vector3.zero)
        {
            GameObject hitVFX = Instantiate(hitPrefab, HitPoint, Quaternion.LookRotation(HitNormal));
            Destroy(hitVFX, 1.0f);
        }
        Destroy(gameObject);
    }
}
