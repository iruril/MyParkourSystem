using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera _myCamera { get; set; } = null;

    private CharacterController _player;
    private PlayerStatus _playerStat;
    private PlayerParkour _playerParkour;
    private GroundChecker _myGroundChecker;
    private PlayerAnimatorFSM _myAnimFSM;

    private Vector3 originCenter = new Vector3(0f, 1f, 0f);
    private float originHeight = 1.6f; 
    private Vector3 newCenter = new Vector3(0f, 1.4f, 0f);
    private float newHeight = 0.8f;

    public Vector3 PlayerVelocity { get; set; } = Vector3.zero;
    public bool MyIsGrounded = false;

    private float _horizontalInput;
    private float _verticalInput;
    private float _rotationHorizontalInput;
    private float _rotationVerticalInput;
    private float _playerYAngleOffset = 0;
    private Vector3 _playerMoveOrientedForward;
    private Vector3 _playerMoveOrientedRight;
    private Quaternion _playerRotation;
    private bool _isRotating;
    public bool IsOnDynamicMove { get; set; } = false;
    public bool IsJumping { get; set; } = false;
    public PlayerParkour.JumpState JumpMode = PlayerParkour.JumpState.None;

    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _myAnimFSM = this.GetComponent<PlayerAnimatorFSM>();
        _playerYAngleOffset = _myCamera.GetComponent<CameraController>().GetCameraOffsetAngles().y;
        _playerMoveOrientedForward = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.forward;
        _playerMoveOrientedRight = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.right;
    }

    void Update()
    {
        GetInput();
        CalculatePlayerTransformByInput(); //Calculated PlayerVelocity is based on 'FixedTime', so we smooth this with 'Time'.
    }

    private void FixedUpdate() //For Velocity Calculations
    {
        MyIsGrounded = _myGroundChecker.IsGrounded();
        if (!IsOnDynamicMove) //While Isn't on Parkour Action
        {
            if (_isRotating)
            {
                PlayerRotate();
            }
            PlayerMove();
        }
    }

    #region Player Input Fields
    private void GetInput()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");
        _rotationHorizontalInput = Input.GetAxisRaw("Horizontal");
        _rotationVerticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            IsJumping = true;
        }
    }

    private void CalculatePlayerTransformByInput()
    {
        if (MyIsGrounded)
        {
            JumpMode = _playerParkour.CheckRay(); //Predicts Next JumpMode
        }

        if (_rotationHorizontalInput != 0 || _rotationVerticalInput != 0)
        {
            _playerRotation = Quaternion.Euler(0, CalculateRotationAngle(_rotationHorizontalInput, _rotationVerticalInput), 0);
            _isRotating = true;
        }
        _player.Move(PlayerVelocity * Time.deltaTime);
    }
    #endregion

    #region Player Default Move And Rotation Fields
    private void PlayerMove() //Combine XZ-Plane Velocity and Y-Axis Velocity. 
    {
        Vector3 xzPlaneVel = PlayerXZPlaneVelocity(); //Calculates XZ-Plane Velocity
        float yAxisVel = PlayerYAxisVelocity(); //Calculates Y-Axis Velocity
        PlayerVelocity = new Vector3(xzPlaneVel.x, yAxisVel, xzPlaneVel.z); //Combine
    }

    private Vector3 PlayerXZPlaneVelocity() //Calculates XZ-Plane Velocity By Player's Input
    {
        Vector3 moveVel = _playerMoveOrientedForward * _verticalInput + _playerMoveOrientedRight * _horizontalInput;
        Vector3 moveDir = moveVel.normalized; //Direction

        float moveSpeed = Mathf.Min(moveVel.magnitude, 1.0f) * _playerStat.MyCurrentSpeed;

        return moveDir * moveSpeed;
    }

    private float PlayerYAxisVelocity() //Calculates Y-Axis Velocity By Player's Input
    {
        if (!MyIsGrounded) // If isn't on ground, then apply Gravity force
        {
            if (IsJumping) //while get input 'Jump'
            {
                switch (JumpMode)
                {
                    default:
                        IsJumping = false;
                        break;
                }
            }
            return PlayerVelocity.y - _playerStat.GravityForce * Time.fixedDeltaTime;
        }

        if (IsJumping) //while get input 'Jump'
        {
            switch (JumpMode)
            {
                case PlayerParkour.JumpState.DefaultJump: //Do Default Jump
                    IsJumping = false;
                    return PlayerVelocity.y + _playerStat.JumpPower;
                case PlayerParkour.JumpState.Vault: //Do Vault Action
                    if (!IsOnDynamicMove)
                    {
                        StartCoroutine(DoVault());
                    }
                    else
                    {
                        JumpMode = PlayerParkour.JumpState.None;
                        IsJumping = false;
                    }
                    return Mathf.Max(0.0f, PlayerVelocity.y);
                case PlayerParkour.JumpState.JumpClimb: //Do JumpClimb Action
                    if (!IsOnDynamicMove)
                    {
                        StartCoroutine(DoHopClimb());
                    }
                    else
                    {
                        JumpMode = PlayerParkour.JumpState.None;
                        IsJumping = false;
                    }
                    return Mathf.Max(0.0f, PlayerVelocity.y); //Do Climb Action
                case PlayerParkour.JumpState.Climb:
                    IsJumping = false;
                    return PlayerVelocity.y + _playerStat.JumpPower;
                default:
                    IsJumping = false;
                    return Mathf.Max(0.0f, PlayerVelocity.y); //If There's no condition
            }
        }
        else
        {
            return Mathf.Max(0.0f, PlayerVelocity.y); //Default Y-Axis Velocity
        }
    }

    private float CalculateRotationAngle(float h, float v) //Calculates Radian-Angle By Vector2(h,v)
    {
        return _playerYAngleOffset + Mathf.Atan2(h, v) * Mathf.Rad2Deg;
    }

    private void PlayerRotate() //Do Character Rotation By Player's Input
    {
        if (PlayerVelocity.magnitude < 0.1f || (_rotationVerticalInput == 0 && _rotationHorizontalInput == 0))
        {
            _isRotating = false;
            return;
        }

        float step = Time.fixedDeltaTime * _playerStat.RotateSpeed; //Smooth Step for Prevent Ragging
        this.transform.rotation = Quaternion.Slerp(transform.rotation, _playerRotation, step);
    }
    #endregion

    #region Parkour Actions Fields
    private IEnumerator DoVault()
    {
        if (JumpMode != PlayerParkour.JumpState.Vault && !_myGroundChecker.IsGrounded())
        {
            IsJumping = false;
            yield break;
        }

        //Animation Set
        _myAnimFSM.PrevState = _myAnimFSM.CurrentState;
        _myAnimFSM.NextState = PlayerAnimatorFSM.STATE.PARKOUR_VAULT;
        Animator anim = _myAnimFSM.MyAnimator;
        anim.Play(anim.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0);

        //Default Value Set
        PlayerVelocity = Vector3.zero;
        IsOnDynamicMove = true;
        Vector3 startPoint = this.transform.position;
        Vector3 endPoint = _playerParkour.StepPoint;
        endPoint.y = endPoint.y - 0.7f;
        _player.height = newHeight;
        _player.center = newCenter;

        //First Step Action (Move to Step Point)
        float lerpTime = _playerParkour.ParkourJumpTime;
        float currentTime = 0;
        while (currentTime < _playerParkour.ParkourJumpTime)
        {
            currentTime += Time.deltaTime;
            this.transform.position = Vector3.Slerp(startPoint, endPoint, currentTime / lerpTime);
            yield return null;
        }
        //Second Step Action

        Vector3 VaultDir = this.transform.forward;
        float yAxisVel = Mathf.Max(0.0f, this.transform.position.y);
        lerpTime = _playerParkour.ParkourVaultTime - _playerParkour.ParkourJumpTime;
        currentTime = 0;
        while (currentTime < lerpTime)
        {
            currentTime += Time.deltaTime;
            PlayerVelocity = new Vector3(VaultDir.x *_playerParkour.ParkourMoveSpeed,
                                         Mathf.Max(0.0f, PlayerVelocity.y),
                                         VaultDir.z *_playerParkour.ParkourMoveSpeed);
            _player.height = Mathf.Lerp(newHeight, originHeight, currentTime / lerpTime);
            _player.center = Vector3.Lerp(newCenter, originCenter, currentTime / lerpTime);
            yield return null;
        }
        IsJumping = false;
        IsOnDynamicMove = false;
        JumpMode = PlayerParkour.JumpState.None;
        yield break;
    }

    private IEnumerator DoHopClimb()
    {
        if (JumpMode != PlayerParkour.JumpState.JumpClimb && !_myGroundChecker.IsGrounded())
        {
            IsJumping = false;
            yield break;
        }

        //Animation Set
        _myAnimFSM.PrevState = _myAnimFSM.CurrentState;
        _myAnimFSM.NextState = PlayerAnimatorFSM.STATE.PARKOUR_JUMP_CLIMB;
        Animator anim = _myAnimFSM.MyAnimator;
        anim.Play(anim.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0);

        //Default Value Set
        PlayerVelocity = Vector3.zero;
        IsOnDynamicMove = true;
        Vector3 startPoint = this.transform.position;
        Vector3 endPoint = _playerParkour.StepPoint;
        Vector3 t1 = new Vector3(endPoint.x, 0, endPoint.z);
        Vector3 t2 = new Vector3(startPoint.x, 0, startPoint.z);
        Vector3 climbPoint = _playerParkour.StepPoint - (t1 - t2).normalized * 0.3f;
        climbPoint.y = climbPoint.y - 0.9f;
        _player.height = newHeight;
        _player.center = newCenter;

        //First Step Action (Move to Step Point)
        float lerpTime = _playerParkour.ParkourClimbTime;
        float currentTime = 0;
        while (currentTime < _playerParkour.ParkourClimbTime)
        {
            currentTime += Time.deltaTime;
            this.transform.position = Vector3.Slerp(startPoint, climbPoint, currentTime / lerpTime);
            
            yield return null;
        }

        //Second Step Action
        lerpTime = _playerParkour.ParkourJumpClimbTime - _playerParkour.ParkourClimbTime;
        currentTime = 0;
        while (currentTime < lerpTime)
        {
            currentTime += Time.deltaTime;
            this.transform.position = Vector3.Slerp(climbPoint, endPoint, currentTime / lerpTime);
            
            yield return null;
        }
        _player.height = originHeight;
        _player.center = originCenter;
        IsJumping = false;
        IsOnDynamicMove = false;
        JumpMode = PlayerParkour.JumpState.None;
        yield break;
    }
    #endregion
}
