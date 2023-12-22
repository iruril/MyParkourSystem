using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    public float Speed = 5f;
    public float MyCurrentSpeed { get; set; } = 0;
    public float MyCurrentSpeedOnAim { get; set; } = 0;
    public float SpeedMultiplyOnAim = 0.4f;
    public float JumpPower = 5f;
    public float GravityForce = 9.8f;
    public float RotateSpeed = 30.0f;

    public AttackStatus AttackStatus = new();

    public float MyStartingHealth = 100.0f;
    public float Health { get; set; }

    public bool Dead = false;
    public float DealtDamage;

    #region Player Movement Management Componenets
    private PlayerController _player;
    private CharacterController _playerCharacter;
    private PlayerAnimationController _playerAnimation;
    #endregion

    private RagdollController _myRagdoll; //If Character is dead, then activates Ragdoll

    void Start()
    {
        _player = this.GetComponent<PlayerController>();
        _playerAnimation = this.GetComponent<PlayerAnimationController>();
        _playerCharacter = this.GetComponent<CharacterController>();
        _myRagdoll = this.GetComponent<RagdollController>();

        MyCurrentSpeed = Speed;
        MyCurrentSpeedOnAim = MyCurrentSpeed * SpeedMultiplyOnAim;
        Health = MyStartingHealth;
        StartCoroutine(WeaponInitialize());
    }

    private IEnumerator WeaponInitialize()
    {
        yield return new WaitUntil(() => GameManager.Instance.IsWeaponListLoaded);
        AttackStatus.SwapStat(GameManager.Instance.WeaponList[0]);
    }

    public void WeaponSwap(int weaponIndex)
    {
        if (!GameManager.Instance.IsWeaponListLoaded) return;
        if (_player.IsReloading) return;
        if (GameManager.Instance.WeaponList[weaponIndex] != null)
        {
            AttackStatus.SwapStat(GameManager.Instance.WeaponList[weaponIndex]);
        }
        else
        {
            Debug.Log("�ش� �ε����� ���� ���Ⱑ �����ϴ�!!");
        }
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
        _player.enabled = false;
        _myRagdoll.SetMyRagdollState(kinematicState: false);
        _myRagdoll.SetMyRagdollCollisionState(collisionRecieveState: true);
        _playerCharacter.enabled = false;
        _playerAnimation.MyAnimator.enabled = false;
        _playerAnimation.enabled = false;
    }
}
