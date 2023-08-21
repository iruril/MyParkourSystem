using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera _myCamera { get; set; } = null;
    [SerializeField] private float _gravityForce = 9.8f;

    private CharacterController _player;
    private Rigidbody _myRigid;
    private PlayerStatus _playerStat;
    private GroundChecker _myGroundChecker;

    private Vector3 _playerDirection = Vector3.forward;
    private Vector3 _playerVelocity = Vector3.zero;

    private Vector3 _prevPosition;
    private Vector3 _nextPosition;

    private float _horizontalInput;
    private float _verticalInput;
    private float _playerYAngleOffset = 0;
    private bool _isJumping = false;

    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _myRigid = this.GetComponent<Rigidbody>();
        _playerStat = this.GetComponent<PlayerStatus>();
        _myGroundChecker = this.GetComponent<GroundChecker>();
        _prevPosition = this.transform.position;
        _nextPosition = this.transform.position;
        _playerYAngleOffset = _myCamera.GetComponent<CameraController>().GetCameraOffsetAngles().y;
    }

    void Update()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isJumping = true;
        }
        float interpolationValue = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        _player.Move(Vector3.Lerp(_prevPosition,_nextPosition, interpolationValue) - transform.position);
    }

    private void FixedUpdate()
    {
        _prevPosition = _nextPosition;
        PlayerMove();
        PlayerRotate();
        _nextPosition += _playerDirection * Time.fixedDeltaTime;
    }

    private void PlayerMove()
    {
        Debug.Log(_myGroundChecker.IsGrounded());
        if (_myGroundChecker.IsGrounded())
        {
            if (_verticalInput != 0 || _horizontalInput != 0)
            {
                _playerDirection = new Vector3(_horizontalInput, 0, _verticalInput);
                _playerVelocity = _playerDirection;
                _playerDirection = _playerDirection.normalized;  
            }
            else
            {
                _playerDirection = new Vector3(0, 0, 0);
                _playerVelocity = _playerDirection;
            }
            _playerDirection = Quaternion.Euler(0, 45, 0) * _playerDirection;
            float currSpeed = Mathf.Min(_playerVelocity.magnitude, 1.0f) * _playerStat.MyCurrentSpeed;
            _playerDirection *= currSpeed;

            if (_isJumping)
            {
                _isJumping = false;
                _playerDirection.y += _playerStat.JumpPower;
            }
        }
        else
        {
            _playerDirection.y -= _gravityForce * Time.fixedDeltaTime;
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
