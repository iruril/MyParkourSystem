using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletGenerator : MonoBehaviour
{
    public ParticleSystem MuzzleFlash = null;
    public GameObject Bullet = null;

    [SerializeField] private LayerMask _IgnoreRaycast;
    [SerializeField] private float fireTimeGap = 1.0f;
    private Vector3 _aimPosition;
    private Vector3 _aimNormal;

    void Start()
    {
        StartCoroutine(Shoot());
    }

    private IEnumerator Shoot()
    {
        Ray aimPointRay = new Ray(this.transform.position, this.transform.forward * 20f);
        RaycastHit hitInfo;

        if (Physics.Raycast(aimPointRay, out hitInfo, 50f, ~_IgnoreRaycast))
        {
            Vector3 aimPos = hitInfo.point;
            _aimPosition = aimPos;
            _aimNormal = hitInfo.normal;
        }

        MuzzleFlash.Play();
        GameObject bullet = Instantiate(Bullet, this.transform.position, this.transform.rotation);
        bullet.GetComponent<ProjectileControl>().Initailize(20f, _aimPosition, _aimNormal);
        yield return YieldCache.WaitForSeconds(fireTimeGap);
        StartCoroutine(Shoot());
    }
}
