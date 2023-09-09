using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    public float Speed = 5f;
    public float MyCurrentSpeed { get; set; } = 0;
    public float JumpPower = 5f;
    public float GravityForce = 9.8f;
    public float RotateSpeed = 30.0f;
    public float WeaponDamage = 30.0f;

    public float MyStartingHealth = 100.0f;
    public float Health { get; set; }

    public bool Dead = false;
    public float DealtDamage;

    #region Player Death Effect Gameobjects
    public GameObject MyHead = null;
    public GameObject MyWeaponGO = null;
    public GameObject GoreParticle = null;
    #endregion

    #region Player Movement Management Componenets
    private Animator _animator;
    private PlayerController _player;
    private CharacterController _playerCharacter;
    private PlayerAnimationController _playerAnimation;
    #endregion

    private RagdollController _myRagdoll; //If Character is dead, then activates Ragdoll

    void Start()
    {
        _animator = this.GetComponent<Animator>();
        _player = this.GetComponent<PlayerController>();
        _playerAnimation = this.GetComponent<PlayerAnimationController>();
        _playerCharacter = this.GetComponent<CharacterController>();
        _myRagdoll = this.GetComponent<RagdollController>();

        MyCurrentSpeed = Speed;
        Health = MyStartingHealth;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            this.Die();
        }
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

        _player.MuzzleFlash.SetActive(false);
        _player.enabled = false;
        _playerAnimation.enabled = false;
        _myRagdoll.SetMyRagdollState(kinematicState: false);
        _myRagdoll.SetMyRagdollCollisionState(collisionRecieveState: true);
        _playerCharacter.enabled = false;
        _animator.enabled = false;
    }
}
