using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimationController : MonoBehaviour
{
    public float TransitionTime = 0.5f;
    private float _lookAtTransitionTime = 0.1f;

    public Animator MyAnimator;
    [SerializeField] private MultiAimConstraint _myBodyAimIK;
    [SerializeField] private MultiAimConstraint _myAimIK;
    [SerializeField] private MultiAimConstraint _myHeadAimIK;
    [SerializeField] private TwoBoneIKConstraint _myLeftArmIK;
    [SerializeField] private MultiAimConstraint _myHeadLookAtIK;

    private PlayerController _player;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;
    private TPSCamController _myTPSCam;

    private int _vaultType = 0;
    private float _mySpeed = 0f;

    private float _currentWeight = 0.0f;
    private float _targetWeight = 0.0f;

    private float _currentLookAtWeight = 0.0f;
    private float _targetLookAtWeight = 0.0f;

    private WaitForSeconds _triggerResetTime;
    private bool _isOnAction = false;

    private Coroutine _animCoroutine = null;

    void Awake()
    {
        _player = this.GetComponent<PlayerController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myTPSCam = this.GetComponent<TPSCamController>();
    }

    private void Start()
    {
        _triggerResetTime = YieldCache.WaitForSeconds(_playerParkour.ParkourJumpTime);
    }

    private void LateUpdate()
    {
        ObserveIKState();

        switch (_player.CurrentMode)
        {
            case PlayerController.MoveMode.Default:
                GetPlayerActionBoolean();
                PlayDefaultAnimation();
                break;
            case PlayerController.MoveMode.Aim:
                GetPlayerSpeedOnAim();
                break;
        }
    }

    private void PlayDefaultAnimation()
    {
        if (!_player.IsOnDynamicMove) GetPlayerSpeed();

        if (_player.IsSpaceKeyAction)
        {
            switch (_player.JumpMode)
            {
                case PlayerParkour.JumpState.DefaultJump:
                    if (!_isOnAction)
                    {
                        DoAction(DefaultJumpCoroutine());
                    }
                    return;
                case PlayerParkour.JumpState.Vault:
                    if (!_player.IsOnDynamicMove && !_isOnAction)
                    {
                        MyAnimator.SetFloat("Speed", _mySpeed);
                        DoAction(VaultCoroutine());
                    }
                    return;
                case PlayerParkour.JumpState.JumpClimb:
                    if (!_player.IsOnDynamicMove && !_isOnAction)
                    {
                        MyAnimator.SetFloat("Speed", _mySpeed);
                        DoAction(JumpClimbCoroutine());
                    }
                    return;
            }
        }
    }

    private void DoAction(IEnumerator action)
    {
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(action);
    }

    #region Basic IK-Control Fields
    private void ObserveIKState()
    {
        _targetWeight = (_player.CurrentMode == PlayerController.MoveMode.Aim) ? 1.0f : 0.0f;
        _currentWeight = Mathf.Lerp(_currentWeight, _targetWeight, Time.deltaTime / TransitionTime);

        MyAnimator.SetLayerWeight(1, _currentWeight);
        _myBodyAimIK.weight = _currentWeight;
        _myAimIK.weight = _currentWeight;
        _myHeadAimIK.weight = _currentWeight;
        _myLeftArmIK.weight = _currentWeight;

        _targetLookAtWeight = (_myTPSCam.IsCamInSight && _mySpeed == 0) ? 0.75f : 0f;
        _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, _targetLookAtWeight, Time.deltaTime / _lookAtTransitionTime);

        _myHeadLookAtIK.weight = _currentLookAtWeight;
    }
    #endregion

    #region 'Run Layer' Animator Value Set Fields
    private void GetPlayerSpeed()
    {
        Vector3 zxPlaneConvertVec = new Vector3(_player.PlayerVelocity.x, 0f, _player.PlayerVelocity.z);
        _mySpeed = zxPlaneConvertVec.magnitude / _playerStat.Speed;
        MyAnimator.SetFloat("Speed", _mySpeed);
    }

    private void GetPlayerActionBoolean()
    {
        MyAnimator.SetBool("IsGrounded", _player.MyGroundChecker.IsGround);
        MyAnimator.SetBool("IsOnDynamic", _player.IsOnDynamicMove);
    }

    private void SetJumpClimbType()
    {
        switch (_playerParkour.StepMode)
        {
            case PlayerParkour.StepState.Lowest:
                MyAnimator.SetFloat("ClimbType", 0f);
                break;
            case PlayerParkour.StepState.Middle:
                MyAnimator.SetFloat("ClimbType", 0.5f);
                break;
            case PlayerParkour.StepState.Highest:
                MyAnimator.SetFloat("ClimbType", 1f);
                break;
        }
    }

    private void SetVaultType()
    {
        _vaultType = Random.Range(0, 2);
        MyAnimator.SetFloat("VaultType", _vaultType);
    }

    private IEnumerator DefaultJumpCoroutine()
    {
        _isOnAction = true;
        MyAnimator.SetTrigger("Jump");
        yield return _triggerResetTime;
        _isOnAction = false;
        MyAnimator.ResetTrigger("Jump");
    }

    private IEnumerator VaultCoroutine()
    {
        SetVaultType();
        _isOnAction = true;
        MyAnimator.SetTrigger("Vault"); 
        yield return _triggerResetTime; 
        _isOnAction = false;
        MyAnimator.ResetTrigger("Vault");
    }

    private IEnumerator JumpClimbCoroutine()
    {
        SetJumpClimbType();
        _isOnAction = true;
        MyAnimator.SetTrigger("JumpClimb");
        yield return _triggerResetTime;
        _isOnAction = false;
        MyAnimator.ResetTrigger("JumpClimb");
    }
    #endregion

    #region 'Aim Layer' Animator Value Set Fields
    private void GetPlayerSpeedOnAim()
    {
        MyAnimator.SetFloat("SpeedForward", _player.VerticalInput);
        MyAnimator.SetFloat("SpeedRight", _player.HorizontalInput);
    }
    #endregion
}
