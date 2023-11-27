using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimationController : MonoBehaviour
{
    public float DistanceGround = 0.5f;
    public float TransitionTime = 0.5f;

    [SerializeField] private Animator _animator;
    [SerializeField] private Rig _myRig;
    private PlayerController _player;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;
    private GroundChecker _myGroundChecker;
    private LayerMask _layerMask;

    private int _vaultType = 0;
    private float _stepHeight = 0f;
    private float _mySpeed = 0f;

    private float _animIKWeight = 1.0f;
    private float _animIKWeightLerpTime = 0.5f;
    private bool _ikWeightSet = false;

    private float _currentWeight = 0.0f;
    private float _targetWeight = 0.0f;

    void Awake()
    {
        _player = this.GetComponent<PlayerController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _layerMask = _myGroundChecker.GroundLayer;
    }

    void Update()
    {
        _targetWeight = (_player.CurrentMode == PlayerController.MoveMode.Aim) ? 1.0f : 0.0f;
        _currentWeight = Mathf.Lerp(_currentWeight, _targetWeight, Time.deltaTime / TransitionTime);

        _animator.SetLayerWeight(1, _currentWeight);
        _myRig.weight = _currentWeight;

        switch (_player.CurrentMode)
        {
            case PlayerController.MoveMode.Default:
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
                GetPlayerSpeedOnAim();
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
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _animIKWeight);

                _animator.SetIKPosition(AvatarIKGoal.LeftHand, _playerParkour.LeftStepPoint);
            }
            else if (_player.JumpMode == PlayerParkour.JumpState.JumpClimb &&  _stepHeight > 1.5f)
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
        _vaultType = Random.Range(0, 2);
        _animator.SetFloat("VaultType", _vaultType);
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
