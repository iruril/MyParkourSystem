using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorFSM : MonoBehaviour
{
    public Animator MyAnimator { get; set; }
    public float DistanceGround = 0.5f;
    private PlayerController _playerControl;
    private GroundChecker _myGroundChecker;
    private PlayerParkour _myParkour;
    private LayerMask _layerMask;

    private Vector3 _myPlayerXZVelocity;
    private float _myPlayerScalar;
    private float _myPlayerYVelocity;

    private float _animIKWeight = 1.0f;
    private float _animIKWeightLerpTime = 0.5f;
    private bool _ikWeightSet = false;

    public enum STATE
    {
        NONE = 0,
        IDLE = 1,
        SPRINT_START = 2,
        SPRINT = 3,
        SPRINT_END = 4,
        SPRINT_JUMP = 5,
        JUMP_START = 6,
        JUMP_END = 7,
        PARKOUR_VAULT = 8,
        PARKOUR_JUMP_CLIMB = 9,
        PARKOUR_CLIMB = 10
    }
    public STATE PrevState = STATE.NONE;
    public STATE CurrentState = STATE.NONE;
    public STATE NextState = STATE.NONE;
    public float StateTimer = 0f;

    private void Awake()
    {
        MyAnimator = this.GetComponent<Animator>();
        _myParkour = this.GetComponent<PlayerParkour>();
    }

    void Start()
    {
        _playerControl = this.GetComponent<PlayerController>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _layerMask = _myGroundChecker.GroundLayer;
        this.NextState = STATE.IDLE;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_playerControl.IsOnDynamicMove)
        {
            if (!_ikWeightSet)
            {
                StartCoroutine(WeightDecreaser());
            }
            if (this.CurrentState == STATE.PARKOUR_VAULT)
            {
                MyAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _animIKWeight);

                MyAnimator.SetIKPosition(AvatarIKGoal.LeftHand, _myParkour.LeftStepPoint);
            }
            else if (this.CurrentState == STATE.PARKOUR_JUMP_CLIMB)
            {
                MyAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _animIKWeight);
                MyAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, _animIKWeight);

                MyAnimator.SetIKPosition(AvatarIKGoal.LeftHand, _myParkour.LeftStepPoint);
                MyAnimator.SetIKPosition(AvatarIKGoal.RightHand, _myParkour.RightStepPoint);
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
        MyAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        MyAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

        Ray leftRay = new Ray(MyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 0.5f, Vector3.down);
        Debug.DrawRay(MyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * 0.5f, Vector3.down * (0.5f + DistanceGround), Color.red);
        if (Physics.Raycast(leftRay, out RaycastHit leftHitinfo, DistanceGround + 0.5f, _layerMask))
        {
            if (leftHitinfo.transform.tag == "Walkable")
            {
                Vector3 footPos = leftHitinfo.point;
                footPos.y += DistanceGround;

                MyAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, footPos);
            }
        }

        MyAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        MyAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        Ray rightRay = new Ray(MyAnimator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 0.5f, Vector3.down);
        Debug.DrawRay(MyAnimator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * 0.5f, Vector3.down * (0.5f + DistanceGround), Color.red);
        if (Physics.Raycast(rightRay, out RaycastHit rightHitinfo, DistanceGround + 0.5f, _layerMask))
        {
            if (rightHitinfo.transform.tag == "Walkable")
            {
                Vector3 footPos = rightHitinfo.point;
                footPos.y += DistanceGround;

                MyAnimator.SetIKPosition(AvatarIKGoal.RightFoot, footPos);
            }
        }
    }

    private IEnumerator WeightDecreaser()
    {
        _ikWeightSet = true;
        float time = 0;
        while (time <= _animIKWeightLerpTime) {
            _animIKWeight = Mathf.Lerp(1.0f, 0.3f, time / _animIKWeightLerpTime);
            time += Time.deltaTime;
            yield return null;
        }
        yield break;
    }

    void Update()
    {
        _myPlayerXZVelocity = new Vector3(_playerControl.PlayerVelocity.x, 0, _playerControl.PlayerVelocity.z);
        _myPlayerYVelocity = _playerControl.PlayerVelocity.y;
        _myPlayerScalar = _myPlayerXZVelocity.magnitude;

        this.StateTimer += Time.deltaTime;
        if (this.NextState == STATE.NONE)
        {
            switch (this.CurrentState)
            {
                case STATE.IDLE:
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.Vault && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_VAULT;
                    }

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpClimb && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_CLIMB;
                    }

                    if (_myPlayerScalar > 0.1f && !_playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT_START;
                    }
                    if (_myPlayerScalar > 0.1f && _playerControl.JumpMode == PlayerParkour.JumpState.DefaultJump 
                        && (_playerControl.IsJumping || _myPlayerYVelocity > 0.2))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT_JUMP;
                    }
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.DefaultJump
                        && (_playerControl.IsJumping || _myPlayerYVelocity > 0.2))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.JUMP_START;
                    }
                    break;
                case STATE.SPRINT_START:
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.Vault && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_VAULT;
                    }

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpClimb && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_CLIMB;
                    }

                    if (_playerControl.IsJumping || _myPlayerYVelocity > 0.2)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT_JUMP;
                    }
                    if (_myPlayerScalar > 1 && !_playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT;
                    }
                    if (_myPlayerScalar <= 0.1 && !_playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.IDLE;
                    }
                    break;
                case STATE.SPRINT:
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.Vault && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_VAULT;
                    }

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpClimb && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_CLIMB;
                    }

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.DefaultJump
                        && (_playerControl.IsJumping || _myPlayerYVelocity > 0.2))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT_JUMP;
                    }
                    if (_myPlayerScalar <= 1 && !_playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT_END;
                    }
                    if (_myPlayerScalar < 0.1f && !_playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.IDLE;
                    }
                    break;
                case STATE.SPRINT_END:
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.Vault && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_VAULT;
                    }

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpClimb && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_CLIMB;
                    }
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.DefaultJump
                        && (_playerControl.IsJumping || _myPlayerYVelocity > 0.2))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT_JUMP;
                    }
                    if (_myPlayerScalar > 1 && !_playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT;
                    }
                    if (_myPlayerScalar <= 0.1 && !_playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.IDLE;
                    }
                    break;
                case STATE.SPRINT_JUMP:
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.Vault && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_VAULT;
                    }
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpClimb && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_CLIMB;
                    }
                    if (MyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && _myGroundChecker.IsGrounded())
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT;
                    }
                    break;
                case STATE.JUMP_START:
                    if (_myGroundChecker.IsGrounded())
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.JUMP_END;
                    }
                    break;
                case STATE.JUMP_END:
                    if (_playerControl.JumpMode == PlayerParkour.JumpState.DefaultJump
                        && (_playerControl.IsJumping || _myPlayerYVelocity > 0.2))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.JUMP_START;
                    }
                    else if (_playerControl.JumpMode == PlayerParkour.JumpState.Vault && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_VAULT;
                    }
                    else if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpClimb && _playerControl.IsOnDynamicMove)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_CLIMB;
                    }
                    else
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.IDLE;
                    }
                    break;
                case STATE.PARKOUR_VAULT:
                    if ((MyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && _myGroundChecker.IsGrounded()) ||(
                        MyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && !_playerControl.IsOnDynamicMove))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT;
                    }
                    break;
                case STATE.PARKOUR_JUMP_CLIMB:
                    if ((MyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && _myGroundChecker.IsGrounded()) || (
                        MyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && !_playerControl.IsOnDynamicMove))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT;
                    }
                    break;
                case STATE.PARKOUR_CLIMB:

                    break;
            }
        }

        while (this.NextState != STATE.NONE)
        {
            this.CurrentState = this.NextState;
            this.NextState = STATE.NONE;
            switch (this.CurrentState)
            {
                case STATE.IDLE:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT_START:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT_END:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.JUMP_START:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.JUMP_END:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT_JUMP:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.PARKOUR_VAULT:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.PARKOUR_JUMP_CLIMB:
                    MyAnimator.applyRootMotion = false;
                    MyAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.PARKOUR_CLIMB:

                    break;
            }
            this.StateTimer = 0f;
        }

        switch (this.CurrentState)
        {
            case STATE.IDLE:

                break;
            case STATE.SPRINT_START:

                break;
            case STATE.SPRINT:

                break;
            case STATE.SPRINT_END:

                break;
            case STATE.JUMP_START:

                break;
            case STATE.JUMP_END:

                break;
            case STATE.PARKOUR_VAULT:

                break;
            case STATE.PARKOUR_JUMP_CLIMB:

                break;
            case STATE.PARKOUR_CLIMB:

                break;
        }
    }

    private int StateEnumToInt(STATE state)
    {
        return (int)state;
    }
}
