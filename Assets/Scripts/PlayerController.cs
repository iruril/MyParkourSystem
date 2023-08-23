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

    public Vector3 PlayerVelocity { get; set; } = Vector3.zero;

    private float _horizontalInput;
    private float _verticalInput;
    private float _rotationHorizontalInput;
    private float _rotationVerticalInput;
    private float _playerYAngleOffset = 0;
    private Vector3 _playerMoveOrientedForward;
    private Vector3 _playerMoveOrientedRight;
    private Quaternion _playerRotation;
    private bool _isRotating;
    public bool IsJumping { get; set; } = false;
    public PlayerParkour.JumpState JumpMode = PlayerParkour.JumpState.None;

    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _playerParkour = this.GetComponent<PlayerParkour>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _playerYAngleOffset = _myCamera.GetComponent<CameraController>().GetCameraOffsetAngles().y;
        _playerMoveOrientedForward = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.forward;
        _playerMoveOrientedRight = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.right;
    }

    void Update()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");
        _rotationHorizontalInput = Input.GetAxisRaw("Horizontal");
        _rotationVerticalInput = Input.GetAxisRaw("Vertical");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            IsJumping = true;
        }
        if (_rotationHorizontalInput != 0 || _rotationVerticalInput != 0)
        {
            _playerRotation = Quaternion.Euler(0, CalculateRotationAngle(_rotationHorizontalInput, _rotationVerticalInput), 0);
            _isRotating = true;
        }
        _player.Move(PlayerVelocity * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (_isRotating)
        {
            PlayerRotate();
        }
        PlayerMove();
    }

    #region Player Default Move And Rotation
    private void PlayerMove()
    {
        Vector3 xzPlaneVel = PlayerXZPlaneVelocity();
        float yAxisVel = PlayerYAxisVelocity();
        PlayerVelocity = new Vector3(xzPlaneVel.x, yAxisVel, xzPlaneVel.z);
    }

    private Vector3 PlayerXZPlaneVelocity()
    {
        Vector3 moveVel = _playerMoveOrientedForward * _verticalInput + _playerMoveOrientedRight * _horizontalInput;
        Vector3 moveDir = moveVel.normalized;

        float moveSpeed = Mathf.Min(moveVel.magnitude, 1.0f) * _playerStat.MyCurrentSpeed;

        return moveDir * moveSpeed;
    }

    private float PlayerYAxisVelocity()
    {
        if (!_myGroundChecker.IsGrounded())
        {
            return PlayerVelocity.y - _playerStat.GravityForce * Time.fixedDeltaTime;
        }

        if (IsJumping)
        {
            IsJumping = false;
            JumpMode = _playerParkour.CheckRay();
            return PlayerVelocity.y + _playerStat.JumpPower;
        }
        else
        {
            return Mathf.Max(0.0f, PlayerVelocity.y);
        }
    }

    private float CalculateRotationAngle(float h, float v)
    {
        return _playerYAngleOffset + Mathf.Atan2(h, v) * Mathf.Rad2Deg;
    }

    private void PlayerRotate()
    {
        if (PlayerVelocity.magnitude < 0.1f || (_rotationVerticalInput == 0 && _rotationHorizontalInput == 0))
        {
            _isRotating = false;
            return;
        }

        float step = Time.fixedDeltaTime * _playerStat.RotateSpeed;
        transform.rotation = Quaternion.Slerp(transform.rotation, _playerRotation, step);
    }
    #endregion
}
