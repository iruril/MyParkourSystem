using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatus : MonoBehaviour, IDamageable
{
    public float MyStartingHealth = 100.0f;
    public float Health { get; set; }

    public bool Dead = false;
    public float DealtDamage;

    #region Enemy Death Effect Gameobjects
    public GameObject MyHead = null;
    public GameObject MyWeaponGO = null;
    public GameObject GoreParticle = null;
    #endregion

    #region Enemy Movement Management Componenets
    private Animator _animator;
    private CapsuleCollider _myCollider;
    #endregion

    private RagdollController _myRagdoll; //If Character is dead, then activates Ragdoll

    void Start()
    {
        _animator = this.GetComponent<Animator>();
        _myCollider = this.GetComponent<CapsuleCollider>();
        _myRagdoll = this.GetComponent<RagdollController>();

        Health = MyStartingHealth;
    }
    public virtual void TakeHit(float damage, Vector3 damagedDir)
    {
        Health -= damage;
        DealtDamage = Mathf.Round(damage * 10) * 0.1f;

        if (Health <= 0 && !Dead)
        {
            this.Die();
            this._myRagdoll.DeadEffect(damagedDir);
        }
    }

    private void Die()
    {
        MyHead.SetActive(false);
        GoreParticle.SetActive(true);

        MyWeaponGO.GetComponent<BoxCollider>().enabled = true;
        MyWeaponGO.GetComponent<Rigidbody>().isKinematic = false;
        MyWeaponGO.transform.SetParent(null);

        _myCollider.enabled = false;
        _myRagdoll.SetMyRagdollState(kinematicState: false);
        _myRagdoll.SetMyRagdollCollisionState(collisionRecieveState: true);
        _animator.enabled = false;
    }
}
