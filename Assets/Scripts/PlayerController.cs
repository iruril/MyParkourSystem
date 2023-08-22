using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera _myCamera { get; set; } = null;

    private CharacterController _player;
    private Rigidbody _myRigid;
    private PlayerStatus _playerStat;
    private GroundChecker _myGroundChecker;

    private Vector3 _playerVelocity = Vector3.zero;

    private float _horizontalInput;
    private float _verticalInput;
    private float _playerYAngleOffset = 0;
    private Vector3 _playerMoveOrientedForward;
    private Vector3 _playerMoveOrientedRight;
    private bool _isJumping = false;

    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _myRigid = this.GetComponent<Rigidbody>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _playerYAngleOffset = _myCamera.GetComponent<CameraController>().GetCameraOffsetAngles().y;
        _playerMoveOrientedForward = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.forward;
        _playerMoveOrientedRight = Quaternion.Euler(0, _playerYAngleOffset, 0) * this.transform.right;
    }

    void Update()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isJumping = true;
        }
        _player.Move(_playerVelocity * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        PlayerRotate();
        PlayerMove();
    }

    private void PlayerMove()
    {
        Vector3 xzPlaneVel = PlayerXZPlaneVelocity();
        float yAxisVel = PlayerYAxisVelocity();
        _playerVelocity = new Vector3(xzPlaneVel.x, yAxisVel, xzPlaneVel.z);
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
            return _playerVelocity.y - _playerStat._gravityForce * Time.fixedDeltaTime;
        }

        if (_isJumping)
        {
            _isJumping = false;
            return _playerVelocity.y + _playerStat.JumpPower;
        }
        else
        {
            return Mathf.Max(0.0f, _playerVelocity.y);
        }
    }

    private void PlayerRotate()
    {
        if (_playerVelocity.magnitude < 0.1f || (_verticalInput == 0 && _horizontalInput == 0)) return;
        float v = _verticalInput;
        float h = _horizontalInput;
        this.transform.rotation = Quaternion.Euler(0, _playerYAngleOffset + Mathf.Atan2(h, v) * Mathf.Rad2Deg, 0);
    }
}
