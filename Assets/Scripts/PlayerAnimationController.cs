using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public float DistanceGround = 0.5f;
    public float TransitionTime = 0.5f;
    public Transform LeftHandOnGun = null;
    public Transform RightHandOnGun = null;

    private Animator _animator;
    private PlayerController _player;
    private CharacterController _playerCharacter;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;
    private GroundChecker _myGroundChecker;
    private LayerMask _layerMask;

    private float _vaultType = 0f;
    private float _stepHeight = 0f;
    private float _mySpeed = 0f;

    private float _animIKWeight = 1.0f;
    private float _animIKWeightLerpTime = 0.5f;
    private bool _ikWeightSet = false;


    void Awake()
    {
        _animator = this.GetComponent<Animator>();
        _player = this.GetComponent<PlayerController>();
        _playerCharacter = this.GetComponent<CharacterController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _layerMask = _myGroundChecker.GroundLayer;
    }

    void Update()
    {
        switch (_player.CurrentMode)
        {
            case PlayerController.MoveMode.Default:
                _animator.SetLayerWeight(1, 0.0f); //서서히 증감시킬 것.

                GetPlayerActionBoolean();
                if (!_player.IsOnDynamicMove) GetPlayerSpeed();

                if (_player.IsJumping)
                {
                    switch (_player.JumpMode)
                    {
                        case PlayerParkour.JumpState.DefaultJump:
                            StartCoroutine(DefaultJumpCoroutine());
                            return;
                        case PlayerParkour.JumpState.Vault:
                            if (!_player.IsOnDynamicMove)
                            {
                                _animator.SetFloat("Speed", _mySpeed);
                                SetVaultType();
                                StartCoroutine(VaultCoroutine());
                            }
                            return;
                        case PlayerParkour.JumpState.JumpClimb:
                            if (!_player.IsOnDynamicMove)
                            {
                                _animator.SetFloat("Speed", _mySpeed);
                                SetJumpClimbType();
                                StartCoroutine(JumpClimbCoroutine());
                            }
                            return;
                    }
                }
                break;
            case PlayerController.MoveMode.Aim:
                _animator.SetLayerWeight(1, 1.0f); //서서히 증감시킬 것.

                GetPlayerSpeedOnAim();
                break;
        }
       
    }

    #region Animator Inverse Kinematic Calculation Fields
    private void OnAnimatorIK(int layerIndex)
    {
        if(_player.CurrentMode == PlayerController.MoveMode.Aim)
        {
            _animator.SetLookAtWeight(1.0f);
            _animator.SetLookAtPosition(_player.LookTarget);
            if (LeftHandOnGun != null)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, LeftHandOnGun.position);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, LeftHandOnGun.rotation);
            }
            if (RightHandOnGun != null)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
                _animator.SetIKPosition(AvatarIKGoal.RightHand, RightHandOnGun.position);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
                _animator.SetIKRotation(AvatarIKGoal.RightHand, RightHandOnGun.rotation);
            }
        }
        else
        {
            _animator.SetLookAtWeight(0.0f);
        }

        if (_player.IsOnDynamicMove)
        {
            if (!_ikWeightSet)
            {
                StartCoroutine(WeightDecreaser());
            }
            if (_player.JumpMode == PlayerParkour.JumpState.Vault)
            {
                if (_vaultType == 0)
                {
                    _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _animIKWeight);

                    _animator.SetIKPosition(AvatarIKGoal.LeftHand, _playerParkour.LeftStepPoint);
                }
            }
            else if (_player.JumpMode == PlayerParkour.JumpState.JumpClimb)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _animIKWeight);
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _animIKWeight);

                _animator.SetIKPosition(AvatarIKGoal.LeftHand, _playerParkour.LeftStepPoint);
                _animator.SetIKPosition(AvatarIKGoal.RightHand, _playerParkour.RightStepPoint);
            }
        }
        else
        {
            FootIK();
            _ikWeightSet = false;
        }
    }

    private void FootIK()
    {
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

        Ray leftRay = new Ray(_animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 0.75f, Vector3.down);
        Debug.DrawRay(_animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 0.75f, Vector3.down * (0.75f + DistanceGround), Color.red);
        if (Physics.Raycast(leftRay, out RaycastHit leftHitinfo, DistanceGround + 0.75f, _layerMask))
        {
            if (leftHitinfo.transform.tag == "Walkable")
            {
                Vector3 footPos = leftHitinfo.point;
                footPos.y += DistanceGround;

                _animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPos);
            }
        }

        _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        Ray rightRay = new Ray(_animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 0.75f, Vector3.down);
        Debug.DrawRay(_animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 0.75f, Vector3.down * (0.75f + DistanceGround), Color.red);
        if (Physics.Raycast(rightRay, out RaycastHit rightHitinfo, DistanceGround + 0.75f, _layerMask))
        {
            if (rightHitinfo.transform.tag == "Walkable")
            {
                Vector3 footPos = rightHitinfo.point;
                footPos.y += DistanceGround;

                _animator.SetIKPosition(AvatarIKGoal.RightFoot, footPos);
            }
        }
    }

    private IEnumerator WeightDecreaser()
    {
        _ikWeightSet = true;
        float time = 0;
        while (time <= _animIKWeightLerpTime)
        {
            _animIKWeight = Mathf.Lerp(1.0f, 0.3f, time / _animIKWeightLerpTime);
            time += Time.deltaTime;
            yield return null;
        }
        yield break;
    }
    #endregion

    #region 'Run Layer' Animator Value Set Fields
    private void GetPlayerSpeed()
    {
        Vector3 zxPlaneConvertVec = new Vector3(_player.PlayerVelocity.x, 0f, _player.PlayerVelocity.z);
        _mySpeed = zxPlaneConvertVec.magnitude / _playerStat.Speed;
        _animator.SetFloat("Speed", _mySpeed);
    }

    private void GetPlayerActionBoolean()
    {
        _animator.SetBool("IsGrounded", _player.MyIsGrounded);
        _animator.SetBool("IsOnDynamic", _player.IsOnDynamicMove);
    }

    private void SetJumpClimbType()
    {
        _stepHeight = _playerParkour.StepHeight;
        if (_stepHeight <= 1.5f)
        {
            _animator.SetFloat("ClimbType", 0f);
        }
        else if(_stepHeight is > 1.5f and <= 2.0f)
        {
            _animator.SetFloat("ClimbType", 0.5f);
        }
        else
        {
            _animator.SetFloat("ClimbType", 1f);
        }
    }

    private void SetVaultType()
    {
        _vaultType = Random.Range(0, 3);
        _animator.SetFloat("VaultType", _vaultType * 0.5f);
    }

    private IEnumerator DefaultJumpCoroutine()
    {
        _animator.SetTrigger("Jump");
        yield return new WaitForSeconds(0.05f);
        _animator.ResetTrigger("Jump");
    }

    private IEnumerator VaultCoroutine()
    {
        _animator.SetTrigger("Vault");
        yield return new WaitForSeconds(0.05f);
        _animator.ResetTrigger("Vault");
    }

    private IEnumerator JumpClimbCoroutine()
    {
        _animator.SetTrigger("JumpClimb");
        yield return new WaitForSeconds(0.05f);
        _animator.ResetTrigger("JumpClimb");
    }
    #endregion

    #region 'Aim Layer' Animator Value Set Fields
    private void GetPlayerSpeedOnAim()
    {
        _animator.SetFloat("SpeedForward", _player.PlayerVelocityBasedOnLookDir.z / (_playerStat.Speed / 2f));
        _animator.SetFloat("SpeedRight", _player.PlayerVelocityBasedOnLookDir.x / (_playerStat.Speed / 2f));
    }
    #endregion
}
