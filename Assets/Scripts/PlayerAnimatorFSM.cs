using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorFSM : MonoBehaviour
{
    private Animator _myAnimator;
    private PlayerController _playerControl;
    private PlayerParkour _playerParkour;
    private GroundChecker _myGroundChecker;

    private Vector3 _myPlayerXZVelocity;
    private float _myPlayerScalar;
    private float _myPlayerYVelocity;

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
        PARKOUR_JUMP_OVER = 8,
        PARKOUR_JUMP_CLIMB = 9,
        PARKOUR_CLIMB = 10
    }
    public STATE PrevState = STATE.NONE;
    public STATE CurrentState = STATE.NONE;
    public STATE NextState = STATE.NONE;
    public float StateTimer = 0f;

    void Start()
    {
        _playerControl = this.GetComponent<PlayerController>();
        _myAnimator = this.GetComponent<Animator>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        this.NextState = STATE.IDLE;
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
                    if(_myPlayerScalar > 0.1f && !_playerControl.IsJumping)
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

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpOver && _playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_OVER;
                    }
                        break;
                case STATE.SPRINT_START:
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

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpOver && _playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_OVER;
                    }
                    break;
                case STATE.SPRINT:
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

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpOver && _playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_OVER;
                    }
                    break;
                case STATE.SPRINT_END:
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

                    if (_playerControl.JumpMode == PlayerParkour.JumpState.JumpOver && _playerControl.IsJumping)
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.PARKOUR_JUMP_OVER;
                    }
                    break;
                case STATE.SPRINT_JUMP:
                    if (_myAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && _myGroundChecker.IsGrounded())
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
                    else
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.IDLE;
                    }
                    break;
                case STATE.PARKOUR_JUMP_OVER:
                    if (_myAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f ||
                        (_myAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f && _myGroundChecker.IsGrounded()))
                    {
                        this.PrevState = this.CurrentState;
                        this.NextState = STATE.SPRINT;
                    }
                    break;
                case STATE.PARKOUR_JUMP_CLIMB:

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
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT_START:
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT:
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT_END:
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.JUMP_START:
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.JUMP_END:
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.SPRINT_JUMP:
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.PARKOUR_JUMP_OVER:
                    _myAnimator.applyRootMotion = true;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
                    break;
                case STATE.PARKOUR_JUMP_CLIMB:
                    _myAnimator.applyRootMotion = false;
                    _myAnimator.SetInteger("State", StateEnumToInt(CurrentState));
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
            case STATE.PARKOUR_JUMP_OVER:

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
