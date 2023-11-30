using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimationController : MonoBehaviour
{
    public float TransitionTime = 0.5f;

    [SerializeField] private Animator _animator;
    [SerializeField] private Rig _myRig;
    private PlayerController _player;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;

    private int _vaultType = 0;
    private float _mySpeed = 0f;

    private float _currentWeight = 0.0f;
    private float _targetWeight = 0.0f;

    private WaitForSeconds _triggerResetTime;
    private bool _isOnAction = false;

    void Awake()
    {
        _player = this.GetComponent<PlayerController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
    }

    private void Start()
    {
        _triggerResetTime = new WaitForSeconds(_playerParkour.ParkourJumpTime);
    }

    void LateUpdate()
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
                            if (!_isOnAction)
                            {
                                StartCoroutine(DefaultJumpCoroutine());
                            }
                            return;
                        case PlayerParkour.JumpState.Vault:
                            if (!_player.IsOnDynamicMove && !_isOnAction)
                            {
                                _animator.SetFloat("Speed", _mySpeed);
                                SetVaultType();
                                StartCoroutine(VaultCoroutine());
                            }
                            return;
                        case PlayerParkour.JumpState.JumpClimb:
                            if (!_player.IsOnDynamicMove && !_isOnAction)
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

    #region 'Run Layer' Animator Value Set Fields
    private void GetPlayerSpeed()
    {
        Vector3 zxPlaneConvertVec = new Vector3(_player.PlayerVelocity.x, 0f, _player.PlayerVelocity.z);
        _mySpeed = zxPlaneConvertVec.magnitude / _playerStat.Speed;
        _animator.SetFloat("Speed", _mySpeed);
    }

    private void GetPlayerActionBoolean()
    {
        _animator.SetBool("IsGrounded", _player.MyGroundChecker.IsGrounded());
        _animator.SetBool("IsOnDynamic", _player.IsOnDynamicMove);
    }

    private void SetJumpClimbType()
    {
        switch (_playerParkour.StepMode)
        {
            case PlayerParkour.StepState.Lowest:
                _animator.SetFloat("ClimbType", 0f);
                break;
            case PlayerParkour.StepState.Middle:
                _animator.SetFloat("ClimbType", 0.5f);
                break;
            case PlayerParkour.StepState.Highest:
                _animator.SetFloat("ClimbType", 1f);
                break;
        }
        //if (_playerParkour.StepHeight <= 1.5f)
        //{
        //    _animator.SetFloat("ClimbType", 0f);
        //}
        //else if(_playerParkour.StepHeight is > 1.5f and <= 2.0f)
        //{
        //    _animator.SetFloat("ClimbType", 0.5f);
        //}
        //else
        //{
        //    _animator.SetFloat("ClimbType", 1f);
        //}
    }

    private void SetVaultType()
    {
        _vaultType = Random.Range(0, 2);
        _animator.SetFloat("VaultType", _vaultType);
    }

    private IEnumerator DefaultJumpCoroutine()
    {
        _isOnAction = true;
        _animator.SetTrigger("Jump");
        yield return _triggerResetTime;
        _isOnAction = false;
        _animator.ResetTrigger("Jump");
    }

    private IEnumerator VaultCoroutine()
    {
        _isOnAction = true;
        _animator.SetTrigger("Vault"); 
        yield return _triggerResetTime; 
        _isOnAction = false;
        _animator.ResetTrigger("Vault");

    }

    private IEnumerator JumpClimbCoroutine()
    {
        _isOnAction = true;
        _animator.SetTrigger("JumpClimb");
        yield return _triggerResetTime;
        _isOnAction = false;
        _animator.ResetTrigger("JumpClimb");
    }
    #endregion

    #region 'Aim Layer' Animator Value Set Fields
    private void GetPlayerSpeedOnAim()
    {
        _animator.SetFloat("SpeedForward", _player.PlayerVelocityOnAim.z / (_playerStat.Speed / 2f));
        _animator.SetFloat("SpeedRight", _player.PlayerVelocityOnAim.x / (_playerStat.Speed / 2f));
    }
    #endregion
}
