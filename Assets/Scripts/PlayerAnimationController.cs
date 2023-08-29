using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public float DistanceGround = 0.5f;

    private Animator _animator;
    private PlayerController _player;
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
    private bool _variableSettedOnDynamic = false;
    private bool _actionEnded = false;
    private bool _isJumped = false;

    void Awake()
    {
        _animator = this.GetComponent<Animator>();
        _player = this.GetComponent<PlayerController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _layerMask = _myGroundChecker.GroundLayer;
    }

    void Update()
    {
        switch (_player.IsOnDynamicMove)
        {
            case true:
                _mySpeed = 0;
                if (!_variableSettedOnDynamic && !_isJumped)
                {
                    if (_player.JumpMode == PlayerParkour.JumpState.Vault)
                    {
                        SetVaultType();
                        _animator.SetTrigger("Vault");
                        _variableSettedOnDynamic = true;
                    }
                    else if (_player.JumpMode == PlayerParkour.JumpState.JumpClimb)
                    {
                        SetJumpClimbType();
                        _animator.SetTrigger("JumpClimb");
                        _variableSettedOnDynamic = true;
                    }
                }
                break;
            case false:
                if (_player.JumpMode == PlayerParkour.JumpState.DefaultJump)
                {
                    if (_player.IsJumping)
                    {
                        StartCoroutine(JumpCoroutine());
                        _isJumped = true;
                    }
                }

                if (_player.MyIsGrounded && _isJumped)
                {
                    _animator.SetTrigger("IsGrounded");
                    _isJumped = false;
                }

                if (_variableSettedOnDynamic)
                {
                    _actionEnded = true;
                    _variableSettedOnDynamic = false;
                }

                if (_player.MyIsGrounded && _actionEnded)
                {
                    _animator.SetTrigger("IsGrounded");
                    _actionEnded = false;
                }

                GetPlayerSpeed();
                break;
        }
    }

    #region Animator Inverse Kinematic Calculation Fields
    private void OnAnimatorIK(int layerIndex)
    {
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

        Ray leftRay = new Ray(_animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 0.5f, Vector3.down);
        Debug.DrawRay(_animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 0.5f, Vector3.down * (0.5f + DistanceGround), Color.red);
        if (Physics.Raycast(leftRay, out RaycastHit leftHitinfo, DistanceGround + 0.5f, _layerMask))
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

        Ray rightRay = new Ray(_animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 0.5f, Vector3.down);
        Debug.DrawRay(_animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 0.5f, Vector3.down * (0.5f + DistanceGround), Color.red);
        if (Physics.Raycast(rightRay, out RaycastHit rightHitinfo, DistanceGround + 0.5f, _layerMask))
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

    #region Animator Float Value Set Fields
    private void GetPlayerSpeed()
    {
        Vector3 zxPlaneConvertVec = new Vector3(_player.PlayerVelocity.x, 0f, _player.PlayerVelocity.z);
        _mySpeed = zxPlaneConvertVec.magnitude / _playerStat.Speed;
        _animator.SetFloat("Speed", _mySpeed);
    }

    private void SetJumpClimbType()
    {
        _stepHeight = _playerParkour.StepHeight;
        if (_stepHeight <= 1.5f)
        {
            _animator.SetFloat("ClimbType", 0f);
        }
        else
        {
            _animator.SetFloat("ClimbType", 1f);
        }
    }

    private void SetVaultType()
    {
        _vaultType = Random.Range(0, 2);
        _animator.SetFloat("VaultType", _vaultType);
    }

    private IEnumerator JumpCoroutine()
    {
        _animator.SetTrigger("Jump");
        yield return new WaitForSeconds(0.45f);
        _animator.ResetTrigger("Jump");
    }
    #endregion
}
