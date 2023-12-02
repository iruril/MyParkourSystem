using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileControl : MonoBehaviour
{
    public GameObject hitPrefab;
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

        ContactPoint contact = co.contacts[0];
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
        Vector3 pos = contact.point;

        if (hitPrefab != null)
        {
            GameObject hitVFX = Instantiate(hitPrefab, pos, rot);
            ParticleSystem particle = hitVFX.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                Destroy(hitVFX, particle.main.duration);
            }
        }
        Destroy(gameObject);
    }
}
