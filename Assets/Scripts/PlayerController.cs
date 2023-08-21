using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera _myCamera { get; set; } = null;
    [SerializeField] private float _gravityForce = 9.8f;

    private CharacterController _player;
    private Rigidbody _myRigid;
    private PlayerStatus _playerStat;

    private Vector3 _playerForward = Vector3.zero;
    private Vector3 _playerRight = Vector3.zero;
    private Vector3 _playerDirection = Vector3.forward;

    public bool IsJumpPressed = false;

    void Start()
    {
        _player = this.GetComponent<CharacterController>();
        _myRigid = this.GetComponent<Rigidbody>();
        _playerStat = this.GetComponent<PlayerStatus>();

        _playerForward = _myCamera.transform.forward;
        _playerForward.y = 0;
        _playerRight = _myCamera.transform.right;
        _playerRight.y = 0;
    }

    void Update()
    {
        PlayerMove();
        PlayerRotate();
    }

    private void PlayerMove()
    {
        if (_player.isGrounded)
        {
            if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
            {
                _playerDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            }
            else
            {
                _playerDirection = new Vector3(0, 0, 0);
            }
            _playerDirection = Quaternion.Euler(0, 45, 0) * _playerDirection;
            _playerDirection *= _playerStat.MyCurrentSpeed;

            if (IsJumpPressed == false && Input.GetButton("Jump"))
            {
                IsJumpPressed = true;
                _playerDirection.y = _playerStat.JumpPower;
            }
        }
        else
        {
            _playerDirection.y -= _gravityForce * Time.deltaTime;
        }

        if (!Input.GetButton("Jump"))
        {
            IsJumpPressed = false;
        }

        _player.Move(_playerDirection * Time.deltaTime);
    }

    private void PlayerRotate()
    {
        if (_player.velocity.magnitude < 0.1f) return;

        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        Quaternion lookDir = Quaternion.Euler(0, 45f + Mathf.Atan2(h, v) * Mathf.Rad2Deg, 0);
        this.transform.rotation = Quaternion.Euler(0, 45f + Mathf.Atan2(h, v) * Mathf.Rad2Deg, 0);
    }
}
